namespace NEventStore
{
    using NEventStore.Dispatcher;

    public sealed class DispatchSchedulerPipelineHook : PipelineHookBase
    {
        private readonly IScheduleDispatches _scheduler;

        public DispatchSchedulerPipelineHook(IScheduleDispatches scheduler = null)
        {
            _scheduler = scheduler ?? new NullDispatcher(); // serves as a scheduler also
        }

        public override void Dispose()
        {
            _scheduler.Dispose();
        }

        public override void PostCommit(ICommit committed)
        {
            if (committed != null)
            {
                _scheduler.ScheduleDispatch(committed);
            }
        }
    }
}