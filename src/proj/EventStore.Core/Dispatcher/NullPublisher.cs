namespace EventStore.Dispatcher
{
	using System;
	using Logging;

	public sealed class NullPublisher : IDispatchCommits,
		IPublishMessages
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(NullPublisher));

		public void Dispose()
		{
			Logger.Debug(Resources.ShuttingDownPublisher);
			GC.SuppressFinalize(this);
		}
		public void Publish(Commit commit)
		{
			Logger.Debug(Resources.PublishingToDevNull);
		}
		public void Dispatch(Commit commit)
		{
			Logger.Info(Resources.PublishingCommit, commit.CommitId);
			this.Publish(commit);
		}
	}
}