namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class OptimisticEventStream : IEventStream
	{
		private readonly ICollection<EventMessage> committed = new LinkedList<EventMessage>();
		private readonly ICollection<EventMessage> events = new LinkedList<EventMessage>();
		private readonly IDictionary<string, object> headers = new Dictionary<string, object>();
		private readonly ICommitEvents persistence;
		private bool disposed;

		public OptimisticEventStream(Guid streamId, ICommitEvents persistence)
		{
			this.StreamId = streamId;
			this.persistence = persistence;
		}
		public OptimisticEventStream(Guid streamId, ICommitEvents persistence, int minRevision, int maxRevision)
			: this(streamId, persistence)
		{
			var commits = persistence.GetFrom(streamId, minRevision, maxRevision);
			this.PopulateStream(minRevision, maxRevision, commits);

			if (minRevision > 0 && this.committed.Count == 0)
				throw new StreamNotFoundException();
		}
		public OptimisticEventStream(Snapshot snapshot, ICommitEvents persistence, int maxRevision)
			: this(snapshot.StreamId, persistence)
		{
			var commits = persistence.GetFrom(snapshot.StreamId, snapshot.StreamRevision, maxRevision);
			this.PopulateStream(snapshot.StreamRevision + 1, maxRevision, commits);
			this.StreamRevision = snapshot.StreamRevision + this.committed.Count;
		}

		protected void PopulateStream(int minRevision, int maxRevision, IEnumerable<Commit> commits)
		{
			foreach (var commit in commits ?? new Commit[0])
			{
				this.CommitSequence = commit.CommitSequence;
				var currentRevision = commit.StreamRevision - commit.Events.Count + 1;
				if (currentRevision > maxRevision)
					return;

				foreach (var @event in commit.Events)
				{
					if (currentRevision > maxRevision)
						break;

					if (currentRevision++ < minRevision)
						continue;

					this.committed.Add(@event);
					this.StreamRevision = currentRevision - 1;
				}
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			this.disposed = true;
		}

		public virtual Guid StreamId { get; private set; }
		public virtual int StreamRevision { get; private set; }
		public virtual int CommitSequence { get; private set; }

		public virtual ICollection<EventMessage> CommittedEvents
		{
			get { return new ImmutableCollection<EventMessage>(this.committed); }
		}
		public virtual ICollection<EventMessage> UncommittedEvents
		{
			get { return new ImmutableCollection<EventMessage>(this.events); }
		}
		public virtual IDictionary<string, object> UncommittedHeaders
		{
			get { return this.headers; }
		}

		public virtual void Add(params EventMessage[] uncommittedEvents)
		{
			this.Add(uncommittedEvents as IEnumerable<EventMessage>);
		}
		
        public virtual void Add(IEnumerable<EventMessage> uncommittedEvents)
		{
			uncommittedEvents = uncommittedEvents ?? new EventMessage[0];
			foreach (var @event in uncommittedEvents.Where(@event => @event != null && @event.Body != null))
				this.events.Add(@event);
		}

		public virtual void CommitChanges(Guid commitId)
		{
			if (!this.HasChanges())
				return;

			try
			{
				this.PersistChanges(commitId);
			}
			catch (ConcurrencyException)
			{
				var commits = this.persistence.GetFrom(this.StreamId, this.StreamRevision + 1, int.MaxValue);
				this.PopulateStream(this.StreamRevision + 1, int.MaxValue, commits);

				throw;
			}
		}
		protected virtual bool HasChanges()
		{
			if (this.disposed)
				throw new ObjectDisposedException(Resources.AlreadyDisposed);

			return this.events.Count > 0;
		}
		protected virtual void PersistChanges(Guid commitId)
		{
			var commit = this.CopyValuesNewCommit(commitId);

			this.persistence.Commit(commit);

			this.PopulateStream(this.StreamRevision + 1, commit.StreamRevision, new[] { commit });
			this.ClearChanges();
		}
		protected virtual Commit CopyValuesNewCommit(Guid commitId)
		{
			return new Commit(
				this.StreamId,
				this.StreamRevision + this.events.Count,
				commitId,
				this.CommitSequence + 1,
				DateTime.UtcNow,
				this.headers.ToDictionary(x => x.Key, x => x.Value),
				this.events.ToList());
		}

		public virtual void ClearChanges()
		{
			this.events.Clear();
			this.headers.Clear();
		}
	}
}