namespace NEventStore.Persistence.SqlPersistence
{
    using System;
    using System.Threading;
    using System.Web;
    using Logging;

    public class ThreadScope<T> : IDisposable where T : class
	{
		private readonly ILog logger = LogFactory.BuildLogger(typeof(ThreadScope<T>));
		private readonly HttpContext context = HttpContext.Current;
		private readonly string threadKey;
		private readonly T current;
		private readonly bool rootScope;
		private bool disposed;

		public ThreadScope(string key, Func<T> factory)
		{
			this.threadKey = typeof(ThreadScope<T>).Name + ":[{0}]".FormatWith(key ?? string.Empty);

			var parent = this.Load();
			this.rootScope = parent == null;
			this.logger.Debug(Messages.OpeningThreadScope, this.threadKey, this.rootScope);

			this.current = parent ?? factory();

			if (this.current == null)
				throw new ArgumentException(Messages.BadFactoryResult, "factory");

			if (this.rootScope)
				this.Store(this.current);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this.disposed)
				return;

			this.logger.Debug(Messages.DisposingThreadScope, this.rootScope);
			this.disposed = true;
			if (!this.rootScope)
				return;

			this.logger.Verbose(Messages.CleaningRootThreadScope);
			this.Store(null);

			var resource = this.current as IDisposable;
			if (resource == null)
				return;

			this.logger.Verbose(Messages.DisposingRootThreadScopeResources);
			resource.Dispose();
		}

		private T Load()
		{
			if (this.context != null)
				return this.context.Items[this.threadKey] as T;

			return Thread.GetData(Thread.GetNamedDataSlot(this.threadKey)) as T;
		}
		private void Store(T value)
		{
			if (this.context != null)
				this.context.Items[this.threadKey] = value;
			else
				Thread.SetData(Thread.GetNamedDataSlot(this.threadKey), value);
		}
		public T Current
		{
			get { return this.current; }
		}
	}
}