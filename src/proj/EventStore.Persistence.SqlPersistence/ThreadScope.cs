namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Threading;
	using System.Web;
	using Logging;

	public class ThreadScope<T> : IDisposable where T : class
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(ThreadScope<T>));
		private static readonly string ThreadKey = typeof(ThreadScope<T>).Name;
		private static readonly bool WebApplication = HttpRuntime.AppDomainId != null;
		private readonly T current;
		private readonly bool rootScope;
		private bool disposed;

		public ThreadScope(Func<T> factory)
		{
			var parent = this[ThreadKey];
			this.rootScope = parent == null;
			Logger.Debug(Messages.OpeningThreadScope, this.rootScope);

			this.current = parent ?? factory();
			if (this.rootScope)
				this[ThreadKey] = this.current;
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

			Logger.Debug(Messages.DisposingThreadScope, this.rootScope);
			this.disposed = true;
			if (!this.rootScope)
				return;

			Logger.Verbose(Messages.CleaningRootThreadScope);
			this[ThreadKey] = null;

			var resource = this.current as IDisposable;
			if (resource == null)
				return;

			Logger.Verbose(Messages.DisposingRootThreadScopeResources);
			resource.Dispose();
		}

		private T this[string key]
		{
			get
			{
				if (WebApplication)
					return HttpContext.Current.Items[key] as T;

				return Thread.GetData(Thread.GetNamedDataSlot(key)) as T;
			}
			set
			{
				if (WebApplication)
					HttpContext.Current.Items[key] = value;

				Thread.SetData(Thread.GetNamedDataSlot(key), value);
			}
		}

		public T Current
		{
			get { return this.current; }
		}
	}
}