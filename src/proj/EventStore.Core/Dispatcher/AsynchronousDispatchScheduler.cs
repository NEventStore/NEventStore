namespace EventStore.Dispatcher
{
	using System;
	using System.Threading;
	using Logging;
	using Persistence;

	public class AsynchronousDispatchScheduler : SynchronousDispatchScheduler
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AsynchronousDispatchScheduler));

		public AsynchronousDispatchScheduler(IDispatchCommits dispatcher, IPersistStreams persistence)
			: base(dispatcher, persistence)
		{
		}

		public override void ScheduleDispatch(Commit commit)
		{
			Logger.Info(Resources.SchedulingDelivery, commit.CommitId);
			ThreadPool.QueueUserWorkItem(x => this.Callback(commit));
		}
		private void Callback(Commit commit)
		{
			base.ScheduleDispatch(commit);
		}
	}
}