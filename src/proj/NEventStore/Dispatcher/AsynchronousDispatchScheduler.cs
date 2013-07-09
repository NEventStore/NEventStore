namespace NEventStore.Dispatcher
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Logging;
    using Persistence;

    public class AsynchronousDispatchScheduler : SynchronousDispatchScheduler
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AsynchronousDispatchScheduler));
        private BlockingCollection<Commit> _queue;
        private int _boundedCapacity = 1024;
        private Task _worker;
        private bool _working;

		public AsynchronousDispatchScheduler(IDispatchCommits dispatcher, IPersistStreams persistence)
			: base(dispatcher, persistence)
		{
		}

        protected override void Start()
        {
            _queue = new BlockingCollection<Commit>(new ConcurrentQueue<Commit>(), _boundedCapacity);
            _worker = new Task(Working);
            _working = true;
            _worker.Start();

            base.Start();
        }

		public override void ScheduleDispatch(Commit commit)
		{
			Logger.Info(Resources.SchedulingDelivery, commit.CommitId);
            _queue.Add(commit);
		}

        void Working()
        {
            while (_working)
            {
                Commit commit = null;
                if (_queue.TryTake(out commit, 100))
                    base.ScheduleDispatch(commit);
            }
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _working = false;
        }

	}

}