namespace NEventStore.Dispatcher
{
    using System;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    public class SynchronousDispatchScheduler : IScheduleDispatches
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (SynchronousDispatchScheduler));
        private readonly IDispatchCommits _dispatcher;
        private readonly IPersistStreams _persistence;
        private bool _disposed;

        public SynchronousDispatchScheduler(IDispatchCommits dispatcher, IPersistStreams persistence)
        {
            _dispatcher = dispatcher;
            _persistence = persistence;

            Logger.Info(Resources.StartingDispatchScheduler);
            Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void ScheduleDispatch(ICommit commit)
        {
            DispatchImmediately(commit);
            MarkAsDispatched(commit);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
            {
                return;
            }

            Logger.Debug(Resources.ShuttingDownDispatchScheduler);
            _disposed = true;
            _dispatcher.Dispose();
            _persistence.Dispose();
        }

        protected virtual void Start()
        {
            Logger.Debug(Resources.InitializingPersistence);
            _persistence.Initialize();

            Logger.Debug(Resources.GettingUndispatchedCommits);
            foreach (var commit in _persistence.GetUndispatchedCommits())
            {
                ScheduleDispatch(commit);
            }
        }

        private void DispatchImmediately(ICommit commit)
        {
            try
            {
                Logger.Info(Resources.SchedulingDispatch, commit.CommitId);
                _dispatcher.Dispatch(commit);
            }
            catch
            {
                Logger.Error(Resources.UnableToDispatch, _dispatcher.GetType(), commit.CommitId);
                throw;
            }
        }

        private void MarkAsDispatched(ICommit commit)
        {
            try
            {
                Logger.Info(Resources.MarkingCommitAsDispatched, commit.CommitId);
                _persistence.MarkCommitAsDispatched(commit);
            }
            catch (ObjectDisposedException)
            {
                Logger.Warn(Resources.UnableToMarkDispatched, commit.CommitId);
            }
        }
    }
}