namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using Logging;

    public class NanoContainer
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(NanoContainer));
		private readonly IDictionary<Type, ContainerRegistration> registrations =
			new Dictionary<Type, ContainerRegistration>();

		public virtual ContainerRegistration Register<TService>(Func<NanoContainer, TService> resolve)
			where TService : class
		{
			Logger.Debug(Messages.RegisteringWireupCallback, typeof(TService));
			var registration = new ContainerRegistration(c => (object)resolve(c));
			this.registrations[typeof(TService)] = registration;
			return registration;
		}

		public virtual ContainerRegistration Register<TService>(TService instance)
		{
			if (Equals(instance, null))
				throw new ArgumentNullException("instance", Messages.InstanceCannotBeNull);

			if (!typeof(TService).IsValueType && !typeof(TService).IsInterface)
				throw new ArgumentException(Messages.TypeMustBeInterface, "instance");

			Logger.Debug(Messages.RegisteringServiceInstance, typeof(TService));
			var registration = new ContainerRegistration(instance);
			this.registrations[typeof(TService)] = registration;
			return registration;
		}

		public virtual TService Resolve<TService>()
		{
			Logger.Debug(Messages.ResolvingService, typeof(TService));

			ContainerRegistration registration;
			if (this.registrations.TryGetValue(typeof(TService), out registration))
				return (TService)registration.Resolve(this);

			Logger.Debug(Messages.UnableToResolve, typeof(TService));
			return default(TService);
		}
	}

	public class ContainerRegistration
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(ContainerRegistration));
		private readonly Func<NanoContainer, object> resolve;
		private object instance;
		private bool instancePerCall;

		public ContainerRegistration(Func<NanoContainer, object> resolve)
		{
			Logger.Verbose(Messages.AddingWireupCallback);
			this.resolve = resolve;
		}
		public ContainerRegistration(object instance)
		{
			Logger.Verbose(Messages.AddingWireupRegistration, instance.GetType());
			this.instance = instance;
		}

		public virtual ContainerRegistration InstancePerCall()
		{
			Logger.Verbose(Messages.ConfiguringInstancePerCall);
			this.instancePerCall = true;
			return this;
		}
		public virtual object Resolve(NanoContainer container)
		{
			Logger.Verbose(Messages.ResolvingInstance);
			if (this.instancePerCall)
			{
				Logger.Verbose(Messages.BuildingNewInstance);
				return this.resolve(container);
			}

			Logger.Verbose(Messages.AttemptingToResolveInstance);

			if (this.instance != null)
				return this.instance;

			Logger.Verbose(Messages.BuildingAndStoringNewInstance);
			return this.instance = this.resolve(container);
		}
	}
}