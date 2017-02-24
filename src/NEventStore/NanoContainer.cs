namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using NEventStore.Logging;
	using System.Reflection;

	public class NanoContainer
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (NanoContainer));

        private readonly IDictionary<Type, ContainerRegistration> _registrations =
            new Dictionary<Type, ContainerRegistration>();

        public virtual ContainerRegistration Register<TService>(Func<NanoContainer, TService> resolve)
            where TService : class
        {
            Logger.Debug(Messages.RegisteringWireupCallback, typeof (TService));
            var registration = new ContainerRegistration(c => (object) resolve(c));
            _registrations[typeof (TService)] = registration;
            return registration;
        }

        public virtual ContainerRegistration Register<TService>(TService instance)
        {
            if (Equals(instance, null))
            {
                throw new ArgumentNullException("instance", Messages.InstanceCannotBeNull);
            }

            if (!typeof(TService).GetTypeInfo().IsValueType && !typeof(TService).GetTypeInfo().IsInterface)
            {
                throw new ArgumentException(Messages.TypeMustBeInterface, "instance");
            }

            Logger.Debug(Messages.RegisteringServiceInstance, typeof (TService));
            var registration = new ContainerRegistration(instance);
            _registrations[typeof (TService)] = registration;
            return registration;
        }

        public virtual TService Resolve<TService>()
        {
            Logger.Debug(Messages.ResolvingService, typeof (TService));

            ContainerRegistration registration;
            if (_registrations.TryGetValue(typeof (TService), out registration))
            {
                return (TService) registration.Resolve(this);
            }

            Logger.Debug(Messages.UnableToResolve, typeof (TService));
            return default(TService);
        }
    }

    public class ContainerRegistration
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (ContainerRegistration));
        private readonly Func<NanoContainer, object> _resolve;
        private object _instance;
        private bool _instancePerCall;

        public ContainerRegistration(Func<NanoContainer, object> resolve)
        {
            Logger.Verbose(Messages.AddingWireupCallback);
            _resolve = resolve;
        }

        public ContainerRegistration(object instance)
        {
            Logger.Verbose(Messages.AddingWireupRegistration, instance.GetType());
            _instance = instance;
        }

        public virtual ContainerRegistration InstancePerCall()
        {
            Logger.Verbose(Messages.ConfiguringInstancePerCall);
            _instancePerCall = true;
            return this;
        }

        public virtual object Resolve(NanoContainer container)
        {
            Logger.Verbose(Messages.ResolvingInstance);
            if (_instancePerCall)
            {
                Logger.Verbose(Messages.BuildingNewInstance);
                return _resolve(container);
            }

            Logger.Verbose(Messages.AttemptingToResolveInstance);

            if (_instance != null)
            {
                return _instance;
            }

            Logger.Verbose(Messages.BuildingAndStoringNewInstance);
            return _instance = _resolve(container);
        }
    }
}