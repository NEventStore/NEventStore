namespace EventStore
{
	using System;
	using System.Collections.Generic;

	public class NanoContainer
	{
		private readonly IDictionary<Type, ContainerRegistration> registrations =
			new Dictionary<Type, ContainerRegistration>();

		public virtual ContainerRegistration Register<TService>(Func<NanoContainer, TService> resolve)
			where TService : class
		{
			var registration = new ContainerRegistration(resolve);
			this.registrations[typeof(TService)] = registration;
			return registration;
		}

		public virtual ContainerRegistration Register<TService>(TService instance)
			where TService : class
		{
			if (instance == null)
				throw new ArgumentNullException("instance", Messages.InstanceCannotBeNull);

			if (!typeof(TService).IsInterface)
				throw new ArgumentException(Messages.TypeMustBeInterface, "instance");

			var registration = new ContainerRegistration(instance);
			this.registrations[typeof(TService)] = registration;
			return registration;
		}

		public virtual TService Resolve<TService>()
			where TService : class
		{
			return this.registrations[typeof(TService)].Resolve(this) as TService;
		}
	}

	public class ContainerRegistration
	{
		private readonly Func<NanoContainer, object> resolve;
		private object instance;
		private bool instancePerCall;

		public ContainerRegistration(object instance)
		{
			this.instance = instance;
		}
		public ContainerRegistration(Func<NanoContainer, object> resolve)
		{
			this.resolve = resolve;
		}

		public virtual ContainerRegistration InstancePerCall()
		{
			this.instancePerCall = true;
			return this;
		}
		public virtual object Resolve(NanoContainer container)
		{
			return this.instancePerCall
				? this.resolve(container)
				: (this.instance ?? (this.instance = this.resolve(container)));
		}
	}
}