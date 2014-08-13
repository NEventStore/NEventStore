namespace NEventStore.Dispatcher
{
    using System;

    [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
    public class NoopDispatcherScheduler : IScheduleDispatches
    {
        public void Dispose()
        {
            // Noop
        }

        public void ScheduleDispatch(ICommit commit)
        {
            // Noop
        }

        public void Start()
        {
            // Noop
        }
    }
}