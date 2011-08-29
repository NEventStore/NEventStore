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
		{
			if (Equals(instance, null))
				throw new ArgumentNullException("instance", Messages.InstanceCannotBeNull);

			if (!typeof(TService).IsValueType && !typeof(TService).IsInterface)
				throw new ArgumentException(Messages.TypeMustBeInterface, "instance");

			var registration = new ContainerRegistration(instance);
			this.registrations[typeof(TService)] = registration;
			return registration;
		}

		public virtual TService Resolve<TService>()
		{
			ContainerRegistration registration;
			if (!this.registrations.TryGetValue(typeof(TService), out registration))
				return default(TService);

			return (TService)registration.Resolve(this);
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

			// With .NET 3.5 passing an argument of type Func<NanoContainer, IPersistStreams> will 
			// resolve to ctor(object) while in .NET 4 it will resolve to ctor(Func<NanoContainer, object>)
			// because it supports covariant return types. In order to support .NET 3.5, a shim is needed
			// to convert between the different delegate signatures.
			if (instance != null && instance is Delegate)
			{
				// Add a shim so that we can invoke the delegate in .NET 3.5
				this.resolve = (NanoContainer c) => ((Delegate)instance).DynamicInvoke(c);
				this.instance = null;
			}
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