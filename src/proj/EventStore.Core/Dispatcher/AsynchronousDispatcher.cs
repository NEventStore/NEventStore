namespace EventStore.Dispatcher
{
	using System;
	using System.Threading;
	using Persistence;

	public class AsynchronousDispatcher : IDispatchCommits
	{
		private readonly IPublishMessages bus;
		private readonly IPersistStreams persistence;
		private bool disposed;

		public AsynchronousDispatcher(IPublishMessages bus, IPersistStreams persistence)
		{
			this.bus = bus;
			this.persistence = persistence;

			this.Start();
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

			this.disposed = true;
			this.bus.Dispose();
			this.persistence.Dispose();
		}

		private void Start()
		{
			this.persistence.Initialize();
			var commits = this.persistence.GetUndispatchedCommits();
			foreach (var commit in commits)
				this.Dispatch(commit);
		}

		public virtual void Dispatch(Commit commit)
		{
			ThreadPool.QueueUserWorkItem(state =>
			{
				this.bus.Publish(commit);
				this.persistence.MarkCommitAsDispatched(commit);
			});
		}
	}
}