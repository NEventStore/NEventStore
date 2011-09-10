namespace EventStore.Dispatcher
{
	using System.Threading;
	using Logging;
	using Persistence;

	public class AsynchronousDispatcher : SynchronousDispatcher
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AsynchronousDispatcher));

		public AsynchronousDispatcher(IPublishMessages bus, IPersistStreams persistence)
			: base(bus, persistence)
		{
		}

		public override void Dispatch(Commit commit)
		{
			Logger.Info(Resources.SchedulingDelivery, commit.CommitId);
			ThreadPool.QueueUserWorkItem(x => this.Callback(commit));
		}
		private void Callback(Commit commit)
		{
			base.Dispatch(commit);
		}
	}
}