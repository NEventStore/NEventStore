namespace NEventStore.Dispatcher
{
    using System;
    using NEventStore.Logging;

    [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
    public sealed class NullDispatcher : IScheduleDispatches, IDispatchCommits
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (NullDispatcher));

        public void Dispatch(ICommit commit)
        {
            Logger.Info(Resources.DispatchingToDevNull);
        }

        public void Dispose()
        {
            Logger.Debug(Resources.ShuttingDownDispatcher);
            GC.SuppressFinalize(this);
        }

        public void ScheduleDispatch(ICommit commit)
        {
            Logger.Info(Resources.SchedulingDispatch, commit.CommitId);
            Dispatch(commit);
        }

        public void Start()
        {
            //No-op
        }
    }
}