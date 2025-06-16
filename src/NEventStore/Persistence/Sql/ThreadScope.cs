namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Threading;
    using NEventStore.Logging;
#if NETFRAMEWORK
    using System.Web;
#endif
    public class ThreadScope<T> : IDisposable where T : class
    {
#if NETFRAMEWORK
        private readonly HttpContext _context = HttpContext.Current;
#endif
        private readonly T _current;
        private readonly ILog _logger = LogFactory.BuildLogger(typeof (ThreadScope<T>));
        private readonly bool _rootScope;
        private readonly string _threadKey;
        private bool _disposed;

        public ThreadScope(string key, Func<T> factory)
        {
            _threadKey = typeof (ThreadScope<T>).Name + ":[{0}]".FormatWith(key ?? string.Empty);

            T parent = Load();
            _rootScope = parent == null;
            _logger.Debug(Messages.OpeningThreadScope, _threadKey, _rootScope);

            _current = parent ?? factory();

            if (_current == null)
            {
                throw new ArgumentException(Messages.BadFactoryResult, "factory");
            }

            if (_rootScope)
            {
                Store(_current);
            }
        }

        public T Current
        {
            get { return _current; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
            {
                return;
            }

            _logger.Debug(Messages.DisposingThreadScope, _rootScope);
            _disposed = true;
            if (!_rootScope)
            {
                return;
            }

            _logger.Verbose(Messages.CleaningRootThreadScope);
            Store(null);

            var resource = _current as IDisposable;
            if (resource == null)
            {
                return;
            }

            _logger.Verbose(Messages.DisposingRootThreadScopeResources);
            resource.Dispose();
        }

        private T Load()
        {
#if NETFRAMEWORK
            if (_context != null)
            {
                return _context.Items[_threadKey] as T;
            }
#endif
            return Thread.GetData(Thread.GetNamedDataSlot(_threadKey)) as T;
        }

        private void Store(T value)
        {
#if NETFRAMEWORK
            if (_context != null)
            {
                _context.Items[_threadKey] = value;
                return;
            }
#endif
            Thread.SetData(Thread.GetNamedDataSlot(_threadKey), value);            
        }
    }
}