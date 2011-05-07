namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Persistence;

	public class OptimisticEventStore : IStoreEvents, ICommitEvents
	{
		private readonly IPersistStreams persistence;
		private readonly IEnumerable<IPipelineHook> pipelineHooks;

		public OptimisticEventStore(IPersistStreams persistence, IEnumerable<IPipelineHook> pipelineHooks)
		{
			this.persistence = persistence;
			this.pipelineHooks = pipelineHooks ?? new IPipelineHook[0];
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				this.persistence.Dispose();
		}

		public virtual IEventStream CreateStream(Guid streamId)
		{
			return new OptimisticEventStream(streamId, this);
		}
		public virtual IEventStream OpenStream(Guid streamId, int minRevision, int maxRevision)
		{
			maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
			return new OptimisticEventStream(streamId, this, minRevision, maxRevision);
		}
		public virtual IEventStream OpenStream(Snapshot snapshot, int maxRevision)
		{
			maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
			return new OptimisticEventStream(snapshot, this, maxRevision);
		}

		IEnumerable<Commit> ICommitEvents.GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			foreach (var commit in this.persistence.GetFrom(streamId, minRevision, maxRevision))
			{
				var filtered = commit;
				foreach (var hook in this.pipelineHooks.Where(x => (filtered = x.Select(filtered)) == null))
					break;

				if (filtered != null)
					yield return filtered;
			}
		}
		void ICommitEvents.Commit(Commit attempt)
		{
			if (!attempt.IsValid() || attempt.IsEmpty())
				return;

			if (this.pipelineHooks.Any(x => !x.PreCommit(attempt)))
				return;

			this.persistence.Commit(attempt);

			foreach (var hook in this.pipelineHooks)
				hook.PostCommit(attempt);
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime started)
		{
			return this.persistence.GetFrom(started);
		}

		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
			return this.persistence.GetSnapshot(streamId, maxRevision); // TODO: add to some kind of cache
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			return this.persistence.AddSnapshot(snapshot); // TODO: update the cache here
		}
		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.persistence.GetStreamsToSnapshot(maxThreshold);
		}
	}
}