namespace NEventStore.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Persistence;

    public class PerformanceCounterPersistenceEngine : IPersistStreams
	{
		public virtual void Initialize()
		{
			this.persistence.Initialize();
		}

		public virtual void Commit(Commit attempt)
		{
			var clock = Stopwatch.StartNew();
			this.persistence.Commit(attempt);
			clock.Stop();

			this.counters.CountCommit(attempt.Events.Count, clock.ElapsedMilliseconds);
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.persistence.MarkCommitAsDispatched(commit);
			this.counters.CountCommitDispatched();
		}

		public IEnumerable<Commit> GetFromTo(DateTime start, DateTime end)
		{
			return this.persistence.GetFromTo(start, end);
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.persistence.GetUndispatchedCommits();
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			return this.persistence.GetFrom(streamId, minRevision, maxRevision);
		}
		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			return this.persistence.GetFrom(start);
		}

		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			var result = this.persistence.AddSnapshot(snapshot);
			if (result)
				this.counters.CountSnapshot();

			return result;
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			return this.persistence.GetSnapshot(streamId, maxRevision);
		}
		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.persistence.GetStreamsToSnapshot(maxThreshold);
		}
		
		public virtual void Purge()
		{
			this.persistence.Purge();
		}

	    public bool IsDisposed
	    {
	        get { return this.persistence.IsDisposed; }
	    }

	    public PerformanceCounterPersistenceEngine(IPersistStreams persistence, string instanceName)
		{
			this.persistence = persistence;
			this.counters = new PerformanceCounters(instanceName);
		}
		~PerformanceCounterPersistenceEngine()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			this.counters.Dispose();
			this.persistence.Dispose();
		}

		private readonly PerformanceCounters counters;
		private readonly IPersistStreams persistence;
	}
}