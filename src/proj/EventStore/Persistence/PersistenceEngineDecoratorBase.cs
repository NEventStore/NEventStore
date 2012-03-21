using System;
using System.Collections.Generic;

namespace EventStore.Persistence
{
	public abstract class PersistenceEngineDecoratorBase : IPersistStreams
	{
		private readonly IPersistStreams _persistence;

		protected PersistenceEngineDecoratorBase(IPersistStreams persistence)
		{
			_persistence = persistence;
		}
		
		public void Dispose()
		{
			_persistence.Dispose();
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			return _persistence.GetFrom(streamId, minRevision, maxRevision);
		}

		public virtual void Commit(Commit attempt)
		{
			_persistence.Commit(attempt);
		}

		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			return _persistence.GetSnapshot(streamId, maxRevision);
		}

		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			return _persistence.AddSnapshot(snapshot);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return _persistence.GetStreamsToSnapshot(maxThreshold);
		}

		public virtual void Initialize()
		{
			_persistence.Initialize();
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			return _persistence.GetFrom(start);
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return _persistence.GetUndispatchedCommits();
		}

		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			_persistence.MarkCommitAsDispatched(commit);
		}

		public virtual void Purge()
		{
			_persistence.Purge();
		}
	}
}
