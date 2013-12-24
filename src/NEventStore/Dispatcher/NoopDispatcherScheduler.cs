namespace NEventStore.Dispatcher
{
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