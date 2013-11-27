namespace NEventStore.Dispatcher
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    public class AsynchronousDispatchScheduler : SynchronousDispatchScheduler
    {
        private const int BoundedCapacity = 1024;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (AsynchronousDispatchScheduler));
        private readonly BlockingCollection<ICommit> _queue;
        private Task _worker;

        public AsynchronousDispatchScheduler(IDispatchCommits dispatcher, IPersistStreams persistence)
            : base(dispatcher, persistence)
        {
            _queue = new BlockingCollection<ICommit>(new ConcurrentQueue<ICommit>(), BoundedCapacity);
        }

        public override void Start()
        {
            _worker = Task.Factory.StartNew(Working);
            base.Start();
        }

        public override void ScheduleDispatch(ICommit commit)
        {
            Logger.Info(Resources.SchedulingDelivery, commit.CommitId);
            _queue.Add(commit);
        }

        private void Working()
        {
            foreach (var commit in _queue.GetConsumingEnumerable())
            {
                base.ScheduleDispatch(commit);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _queue.CompleteAdding();
            if (_worker != null)
            {
                _worker.Wait(TimeSpan.FromSeconds(30));
            }
        }
    }
}