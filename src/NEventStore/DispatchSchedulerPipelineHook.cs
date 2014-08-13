namespace NEventStore
{
    using System;
    using NEventStore.Dispatcher;

    [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
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