namespace EventStore.Dispatcher
{
	using System.Linq;
	using Persistence;

	public class SynchronousDispatcher : IDispatchCommits
	{
		private readonly IPublishMessages bus;
		private readonly IPersistStreams persistence;

		public SynchronousDispatcher(IPublishMessages bus, IPersistStreams persistence)
		{
			this.bus = bus;
			this.persistence = persistence;

			this.Start();
		}
		private void Start()
		{
			foreach (var commit in this.persistence.GetUndispatchedCommits().ToList())
				this.Dispatch(commit);
		}

		public virtual void Dispatch(Commit commit)
		{
			this.bus.Publish(commit);
			this.persistence.MarkCommitAsDispatched(commit);
		}
	}
}