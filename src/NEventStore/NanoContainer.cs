using NEventStore.Logging;
using Microsoft.Extensions.Logging;

namespace NEventStore
{
    /// <summary>
    /// Represents a simple IoC container.
    /// </summary>
    public class NanoContainer
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(NanoContainer));

        private readonly Dictionary<Type, ContainerRegistration> _registrations = [];

        /// <summary>
        /// Registers a service with the container.
        /// </summary>
        public virtual ContainerRegistration Register<TService>(Func<NanoContainer, TService> resolve)
            where TService : class
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Messages.RegisteringWireupCallback, typeof(TService));
            }
            var registration = new ContainerRegistration(c => (object)resolve(c));
            _registrations[typeof(TService)] = registration;
            return registration;
        }

        /// <summary>
        /// Registers a service instance with the container.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
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
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Messages.RegisteringServiceInstance, typeof(TService));
            }
            var registration = new ContainerRegistration(instance);
            _registrations[typeof(TService)] = registration;
            return registration;
        }

        /// <summary>
        /// Resolves a service from the container.
        /// </summary>
        public virtual TService Resolve<TService>()
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Messages.ResolvingService, typeof(TService));
            }

            if (_registrations.TryGetValue(typeof(TService), out ContainerRegistration registration))
            {
                return (TService)registration.Resolve(this);
            }

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Messages.UnableToResolve, typeof(TService));
            }
            return default;
        }
    }

    /// <summary>
    /// Represents a registration in the container.
    /// </summary>
    public class ContainerRegistration
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(ContainerRegistration));
        private readonly Func<NanoContainer, object> _resolve;
        private object _instance;
        private bool _instancePerCall;

        /// <summary>
        /// Initializes a new instance of the ContainerRegistration class.
        /// </summary>
        public ContainerRegistration(Func<NanoContainer, object> resolve)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.AddingWireupCallback);
            }
            _resolve = resolve;
        }

        /// <summary>
        /// Initializes a new instance of the ContainerRegistration class.
        /// </summary>
        public ContainerRegistration(object instance)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.AddingWireupRegistration, instance.GetType());
            }
            _instance = instance;
        }

        /// <summary>
        /// Configures the registration to be resolved once per call.
        /// </summary>
        public virtual ContainerRegistration InstancePerCall()
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.ConfiguringInstancePerCall);
            }
            _instancePerCall = true;
            return this;
        }

        /// <summary>
        /// Resolves the registration.
        /// </summary>
        public virtual object Resolve(NanoContainer container)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.ResolvingInstance);
            }
            if (_instancePerCall)
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace(Messages.BuildingNewInstance);
                }
                return _resolve(container);
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.AttemptingToResolveInstance);
            }

            if (_instance != null)
            {
                return _instance;
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.BuildingAndStoringNewInstance);
            }
            return _instance = _resolve(container);
        }
    }
}