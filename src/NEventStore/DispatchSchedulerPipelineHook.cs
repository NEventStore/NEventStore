namespace NEventStore
{
    using NEventStore.Dispatcher;

    public sealed class DispatchSchedulerPipelineHook : IPipelineHook
    {
        private readonly IScheduleDispatches _scheduler;

        public DispatchSchedulerPipelineHook()
            : this(null)
        {}

        public DispatchSchedulerPipelineHook(IScheduleDispatches scheduler)
        {
            _scheduler = scheduler ?? new NullDispatcher(); // serves as a scheduler also
        }

        public void Dispose()
        {
            _scheduler.Dispose();
        }

        public ICommit Select(ICommit committed)
        {
            return committed;
        }

        public bool PreCommit(CommitAttempt attempt)
        {
            return true;
        }

        public void PostCommit(ICommit committed)
        {
            if (committed != null)
            {
                _scheduler.ScheduleDispatch(committed);
            }
        }
    }
}