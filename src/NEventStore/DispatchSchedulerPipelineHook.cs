namespace NEventStore
{
    using System;
    using NEventStore.Dispatcher;

    public class DispatchSchedulerPipelineHook : IPipelineHook
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Commit Select(Commit committed)
        {
            return committed;
        }

        public virtual bool PreCommit(Commit attempt)
        {
            return true;
        }

        public void PostCommit(Commit committed)
        {
            if (committed != null)
            {
                _scheduler.ScheduleDispatch(committed);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _scheduler.Dispose();
        }
    }
}