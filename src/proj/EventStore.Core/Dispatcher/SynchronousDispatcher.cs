namespace EventStore.Dispatcher
{
	using System;
	using System.Linq;
	using Persistence;

	public class SynchronousDispatcher : IDispatchCommits
	{
		private readonly IPublishMessages bus;
		private readonly IPersistStreams persistence;
		private bool disposed;

		public SynchronousDispatcher(IPublishMessages bus, IPersistStreams persistence)
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

			foreach (var commit in this.persistence.GetUndispatchedCommits())
				this.Dispatch(commit);
		}

		public virtual void Dispatch(Commit commit)
		{
			this.bus.Publish(commit);
			this.persistence.MarkCommitAsDispatched(commit);
		}
	}
}