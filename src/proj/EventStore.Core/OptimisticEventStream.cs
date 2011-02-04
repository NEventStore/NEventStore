namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class OptimisticEventStream : IEventStream
	{
		private readonly ICollection<EventMessage> committed = new LinkedList<EventMessage>();
		private readonly ICollection<EventMessage> uncommitted = new LinkedList<EventMessage>();
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

			if (this.committed.Count == 0)
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
			get { return new ReadOnlyCollection<EventMessage>(this.committed); }
		}
		public virtual ICollection<EventMessage> UncommittedEvents
		{
			get { return new ReadOnlyCollection<EventMessage>(this.uncommitted); }
		}

		public virtual void Add(params EventMessage[] uncommittedEvents)
		{
			if (uncommittedEvents == null || uncommittedEvents.Length == 0)
				throw new ArgumentNullException("uncommittedEvents");

			foreach (var @event in uncommittedEvents)
			{
				if (@event.Body == null)
					throw new ArgumentException(Resources.EventNotPopulated, "uncommittedEvents");

				this.uncommitted.Add(@event);
			}
		}
		public virtual void Add(params object[] uncommittedEvents)
		{
			if (uncommittedEvents == null || uncommittedEvents.Length == 0)
				throw new ArgumentNullException("uncommittedEvents");

			foreach (var @event in uncommittedEvents)
				this.Add(new EventMessage { Body = @event });
		}

		public virtual void CommitChanges(Guid commitId, Dictionary<string, object> headers)
		{
			if (!this.HasChanges())
				return;

			try
			{
				this.PersistChanges(commitId, headers);
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

			return this.uncommitted.Count > 0;
		}
		protected virtual void PersistChanges(Guid commitId, Dictionary<string, object> headers)
		{
			var commit = this.BuildCommit(commitId, headers);

			this.persistence.Commit(commit);

			this.PopulateStream(this.StreamRevision + 1, commit.StreamRevision, new[] { commit });
			this.ClearChanges();
		}
		protected virtual Commit BuildCommit(Guid commitId, Dictionary<string, object> headers)
		{
			return new Commit(
				this.StreamId,
				this.StreamRevision + this.uncommitted.Count,
				commitId,
				this.CommitSequence + 1,
				DateTime.UtcNow,
				headers ?? new Dictionary<string, object>(),
				this.uncommitted.ToList());
		}

		public void ClearChanges()
		{
			this.uncommitted.Clear();
		}
	}
}