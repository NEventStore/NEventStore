namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using NEventStore.Logging;
    using System.Reflection;

    public class NanoContainer
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(NanoContainer));

        private readonly IDictionary<Type, ContainerRegistration> _registrations =
            new Dictionary<Type, ContainerRegistration>();

        public virtual ContainerRegistration Register<TService>(Func<NanoContainer, TService> resolve)
            where TService : class
        {
            if (Logger.IsDebugEnabled) Logger.Debug(Messages.RegisteringWireupCallback, typeof(TService));
            var registration = new ContainerRegistration(c => (object)resolve(c));
            _registrations[typeof(TService)] = registration;
            return registration;
        }

        public virtual ContainerRegistration Register<TService>(TService instance)
        {
            if (Equals(instance, null))
            {
                throw new ArgumentNullException(nameof(instance), Messages.InstanceCannotBeNull);
            }

            if (!typeof(TService).IsValueType && !typeof(TService).IsInterface)
            {
                throw new ArgumentException(Messages.TypeMustBeInterface, nameof(instance));
            }

            if (Logger.IsDebugEnabled) Logger.Debug(Messages.RegisteringServiceInstance, typeof(TService));
            var registration = new ContainerRegistration(instance);
            _registrations[typeof(TService)] = registration;
            return registration;
        }

        public virtual TService Resolve<TService>()
        {
            if (Logger.IsDebugEnabled) Logger.Debug(Messages.ResolvingService, typeof(TService));

            if (_registrations.TryGetValue(typeof(TService), out ContainerRegistration registration))
            {
                return (TService)registration.Resolve(this);
            }

            if (Logger.IsDebugEnabled) Logger.Debug(Messages.UnableToResolve, typeof(TService));
            return default(TService);
        }
    }

    public class ContainerRegistration
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(ContainerRegistration));
        private readonly Func<NanoContainer, object> _resolve;
        private object _instance;
        private bool _instancePerCall;

        public ContainerRegistration(Func<NanoContainer, object> resolve)
        {
            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.AddingWireupCallback);
            _resolve = resolve;
        }

        public ContainerRegistration(object instance)
        {
            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.AddingWireupRegistration, instance.GetType());
            _instance = instance;
        }

        public virtual ContainerRegistration InstancePerCall()
        {
            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.ConfiguringInstancePerCall);
            _instancePerCall = true;
            return this;
        }

        public virtual object Resolve(NanoContainer container)
        {
            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.ResolvingInstance);
            if (_instancePerCall)
            {
                if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.BuildingNewInstance);
                return _resolve(container);
            }

            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.AttemptingToResolveInstance);

            if (_instance != null)
            {
                return _instance;
            }

            if (Logger.IsVerboseEnabled) Logger.Verbose(Messages.BuildingAndStoringNewInstance);
            return _instance = _resolve(container);
        }
    }
}