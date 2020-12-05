namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using NEventStore.Logging;
    using System.Reflection;
    using Microsoft.Extensions.Logging;

    public class NanoContainer
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(NanoContainer));

        private readonly IDictionary<Type, ContainerRegistration> _registrations =
            new Dictionary<Type, ContainerRegistration>();

        public virtual ContainerRegistration Register<TService>(Func<NanoContainer, TService> resolve)
            where TService : class
        {
            Logger.LogDebug(Messages.RegisteringWireupCallback, typeof(TService));
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

            Logger.LogDebug(Messages.RegisteringServiceInstance, typeof(TService));
            var registration = new ContainerRegistration(instance);
            _registrations[typeof(TService)] = registration;
            return registration;
        }

        public virtual TService Resolve<TService>()
        {
            Logger.LogDebug(Messages.ResolvingService, typeof(TService));

            if (_registrations.TryGetValue(typeof(TService), out ContainerRegistration registration))
            {
                return (TService)registration.Resolve(this);
            }

            Logger.LogDebug(Messages.UnableToResolve, typeof(TService));
            return default(TService);
        }
    }

    public class ContainerRegistration
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(ContainerRegistration));
        private readonly Func<NanoContainer, object> _resolve;
        private object _instance;
        private bool _instancePerCall;

        public ContainerRegistration(Func<NanoContainer, object> resolve)
        {
            Logger.LogTrace(Messages.AddingWireupCallback);
            _resolve = resolve;
        }

        public ContainerRegistration(object instance)
        {
            Logger.LogTrace(Messages.AddingWireupRegistration, instance.GetType());
            _instance = instance;
        }

        public virtual ContainerRegistration InstancePerCall()
        {
            Logger.LogTrace(Messages.ConfiguringInstancePerCall);
            _instancePerCall = true;
            return this;
        }

        public virtual object Resolve(NanoContainer container)
        {
            Logger.LogTrace(Messages.ResolvingInstance);
            if (_instancePerCall)
            {
                Logger.LogTrace(Messages.BuildingNewInstance);
                return _resolve(container);
            }

            Logger.LogTrace(Messages.AttemptingToResolveInstance);

            if (_instance != null)
            {
                return _instance;
            }

            Logger.LogTrace(Messages.BuildingAndStoringNewInstance);
            return _instance = _resolve(container);
        }
    }
}