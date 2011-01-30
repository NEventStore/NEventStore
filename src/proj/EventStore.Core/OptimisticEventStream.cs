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
		public OptimisticEventStream(
			Guid streamId,
			ICommitEvents persistence,
			int minRevision,
			int maxRevision,
			IEnumerable<Commit> commits)
			: this(streamId, persistence)
		{
			this.PopulateStream(minRevision, maxRevision, commits);
		}
		private void PopulateStream(int minRevision, int maxRevision, IEnumerable<Commit> commits)
		{
			foreach (var commit in commits ?? new Commit[0])
			{
				var currentRevision = commit.StreamRevision - commit.Events.Count + 1;
				if (currentRevision > maxRevision)
					break;

				foreach (var @event in commit.Events)
				{
					if (currentRevision > maxRevision)
						break;

					if (currentRevision++ < minRevision)
						continue;

					this.committed.Add(@event);
					this.StreamRevision = currentRevision - 1;
					this.CommitSequence = commit.CommitSequence;
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
			get { return new ReadonlyCollection<EventMessage>(this.committed); }
		}
		public virtual ICollection<EventMessage> UncommittedEvents
		{
			get { return new ReadonlyCollection<EventMessage>(this.uncommitted); }
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

		public virtual void CommitChanges(Guid commitId, Dictionary<string, object> headers)
		{
			if (!this.HasChanges())
				return;

			try
			{
				this.ApplyChanges(commitId, headers);
			}
			catch (ConcurrencyException)
			{
				this.UpdateStreamOnException();
				throw;
			}
		}
		private bool HasChanges()
		{
			if (this.disposed)
				throw new ObjectDisposedException(Resources.AlreadyDisposed);

			return this.uncommitted.Count > 0;
		}
		private void ApplyChanges(Guid commitId, Dictionary<string, object> headers)
		{
			var commit = this.BuildCommit(commitId, headers);

			this.persistence.Commit(commit);

			this.StreamRevision = commit.StreamRevision;
			this.CommitSequence = commit.CommitSequence;
			this.uncommitted.Clear();
		}
		private Commit BuildCommit(Guid commitId, Dictionary<string, object> headers)
		{
			return new Commit(
				this.StreamId,
				this.StreamRevision + this.uncommitted.Count,
				commitId,
				this.CommitSequence + 1,
				headers,
				this.uncommitted.ToList());
		}
		private void UpdateStreamOnException()
		{
			var minRevision = this.StreamRevision + 1;
			var commits = this.persistence.GetFrom(this.StreamId, minRevision, int.MaxValue);
			this.PopulateStream(minRevision, int.MaxValue, commits);
		}

		public void ClearChanges()
		{
			this.uncommitted.Clear();
		}
	}
}