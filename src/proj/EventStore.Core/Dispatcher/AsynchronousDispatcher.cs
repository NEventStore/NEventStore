namespace EventStore.Dispatcher
{
	using System;
	using System.Threading;
	using Logging;
	using Persistence;

	public class AsynchronousDispatcher : IDispatchCommits
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AsynchronousDispatcher));
		private readonly IPublishMessages bus;
		private readonly IPersistStreams persistence;
		private bool disposed;

		public AsynchronousDispatcher(IPublishMessages bus, IPersistStreams persistence)
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

			Logger.Info(Resources.StoppingDispatcher);
			this.disposed = true;
			this.bus.Dispose();
			this.persistence.Dispose();
		}

		private void Start()
		{
			Logger.Debug(Resources.InitializingPersistence);
			this.persistence.Initialize();

			Logger.Debug(Resources.GettingUndispatchedCommits);
			var commits = this.persistence.GetUndispatchedCommits();
			foreach (var commit in commits)
				this.Dispatch(commit);
		}

		public virtual void Dispatch(Commit commit)
		{
			Logger.Info(Resources.SchedulingDelivery, commit.CommitId);

			ThreadPool.QueueUserWorkItem(state =>
			{
				Logger.Info(Resources.PublishingCommit, commit.CommitId);
				this.bus.Publish(commit);

				Logger.Info(Resources.MarkingCommitAsDispatched, commit.CommitId);
				this.persistence.MarkCommitAsDispatched(commit);
			});
		}
	}
}