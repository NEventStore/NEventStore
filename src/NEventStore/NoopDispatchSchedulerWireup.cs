namespace NEventStore
{
    using NEventStore.Dispatcher;

    public class NoopDispatchSchedulerWireup : Wireup
    {
        public NoopDispatchSchedulerWireup(Wireup wireup)
            : base(wireup)
        {
            Container.Register<IScheduleDispatches>(c => new NoopDispatcherScheduler());
        }
    }
}