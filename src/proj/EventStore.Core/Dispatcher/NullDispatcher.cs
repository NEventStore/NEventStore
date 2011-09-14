namespace EventStore.Dispatcher
{
	using System;
	using Logging;

	public sealed class NullDispatcher : IScheduleDispatches, IDispatchCommits
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(NullDispatcher));

		public void Dispose()
		{
			Logger.Debug(Resources.ShuttingDownDispatcher);
			GC.SuppressFinalize(this);
		}
		public void ScheduleDispatch(Commit commit)
		{
			Logger.Info(Resources.SchedulingDispatch, commit.CommitId);
			this.Dispatch(commit);
		}
		public void Dispatch(Commit commit)
		{
			Logger.Info(Resources.DispatchingToDevNull);
		}
	}
}