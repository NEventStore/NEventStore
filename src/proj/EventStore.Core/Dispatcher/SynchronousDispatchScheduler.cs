namespace EventStore.Dispatcher
{
	using System;
	using Logging;
	using Persistence;

	public class SynchronousDispatchScheduler : IScheduleDispatches
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(SynchronousDispatchScheduler));
		private readonly IDispatchCommits dispatcher;
		private readonly IPersistStreams persistence;
		private bool disposed;

		public SynchronousDispatchScheduler(IDispatchCommits dispatcher, IPersistStreams persistence)
		{
			this.dispatcher = dispatcher;
			this.persistence = persistence;

			Logger.Info(Resources.StartingDispatchScheduler);
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

			Logger.Debug(Resources.ShuttingDownDispatchScheduler);
			this.disposed = true;
			this.dispatcher.Dispose();
			this.persistence.Dispose();
		}

		protected virtual void Start()
		{
			Logger.Debug(Resources.InitializingPersistence);
			this.persistence.Initialize();

			Logger.Debug(Resources.GettingUndispatchedCommits);
			foreach (var commit in this.persistence.GetUndispatchedCommits())
				this.ScheduleDispatch(commit);
		}

		public virtual void ScheduleDispatch(Commit commit)
		{
			this.DispatchImmediately(commit);
			this.MarkAsDispatched(commit);
		}
		private void DispatchImmediately(Commit commit)
		{
			try
			{
				Logger.Info(Resources.SchedulingDispatch, commit.CommitId);
				this.dispatcher.Dispatch(commit);
			}
			catch
			{
				Logger.Error(Resources.UnableToDispatch, this.dispatcher.GetType(), commit.CommitId);
				throw;
			}
		}
		private void MarkAsDispatched(Commit commit)
		{
			try
			{
				Logger.Info(Resources.MarkingCommitAsDispatched, commit.CommitId);
				this.persistence.MarkCommitAsDispatched(commit);
			}
			catch (ObjectDisposedException)
			{
				Logger.Warn(Resources.UnableToMarkDispatched, commit.CommitId);
			}
		}
	}
}