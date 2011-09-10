namespace EventStore.Dispatcher
{
	using System;
	using Logging;
	using Persistence;

	public class SynchronousDispatcher : IDispatchCommits
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(SynchronousDispatcher));
		private readonly IPublishMessages bus;
		private readonly IPersistStreams persistence;
		private bool disposed;

		public SynchronousDispatcher(IPublishMessages bus, IPersistStreams persistence)
		{
			this.bus = bus;
			this.persistence = persistence;

			Logger.Info(Resources.StartingDispatcher);
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
			Logger.Debug(Resources.InitializingPersistence);
			this.persistence.Initialize();

			Logger.Debug(Resources.GettingUndispatchedCommits);
			foreach (var commit in this.persistence.GetUndispatchedCommits())
				this.Dispatch(commit);
		}

		public virtual void Dispatch(Commit commit)
		{
			Logger.Info(Resources.PublishingCommit, commit.CommitId);
			this.bus.Publish(commit);

			Logger.Info(Resources.MarkingCommitAsDispatched, commit.CommitId);
			this.persistence.MarkCommitAsDispatched(commit);
		}
	}
}