namespace NEventStore.Dispatcher
{
    using System;
    using NEventStore.Logging;

    public sealed class NullDispatcher : IScheduleDispatches, IDispatchCommits
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (NullDispatcher));

        public void Dispatch(Commit commit)
        {
            Logger.Info(Resources.DispatchingToDevNull);
        }

        public void Dispose()
        {
            Logger.Debug(Resources.ShuttingDownDispatcher);
            GC.SuppressFinalize(this);
        }

        public void ScheduleDispatch(Commit commit)
        {
            Logger.Info(Resources.SchedulingDispatch, commit.CommitId);
            Dispatch(commit);
        }
    }
}