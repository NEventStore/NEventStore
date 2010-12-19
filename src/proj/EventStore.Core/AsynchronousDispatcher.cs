namespace EventStore.Core
{
	using System;
	using System.Threading;
	using Dispatcher;
	using Persistence;

	public class AsynchronousDispatcher : IDispatchCommits
	{
		private readonly IPublishMessages bus;
		private readonly IPersistStreams persistence;
		private readonly Action<Commit, Exception> handleException;

		public AsynchronousDispatcher(
			IPublishMessages bus, IPersistStreams persistence, Action<Commit, Exception> handleException)
		{
			this.bus = bus;
			this.persistence = persistence;
			this.handleException = handleException;
		}

		public virtual void Dispatch(Commit commit)
		{
			ThreadPool.QueueUserWorkItem(state => this.BeginDispatch(commit));
		}
		protected virtual void BeginDispatch(Commit commit)
		{
			try
			{
				this.bus.Publish(commit);
				this.persistence.MarkCommitAsDispatched(commit);
			}
			catch (Exception e)
			{
				this.handleException(commit, e);
			}
		}
	}
}