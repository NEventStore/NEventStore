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

		public OptimisticEventStream(
			Guid streamId, int maxAllowedRevision, IEnumerable<Commit> commits, ICommitEvents persistence)
		{
			this.StreamId = streamId;
			this.persistence = persistence;
			this.PopulateStream(maxAllowedRevision, commits);
		}
		private void PopulateStream(int maxAllowedRevision, IEnumerable<Commit> commits)
		{
			var currentRevision = this.StreamRevision;
			var currentSequence = this.CommitSequence;

			foreach (var commit in commits ?? new Commit[0])
			{
				if (currentRevision >= maxAllowedRevision)
					break;

				currentRevision = commit.StreamRevision - commit.Events.Count;
				currentSequence = commit.CommitSequence;

				foreach (var @event in commit.Events)
				{
					if (currentRevision >= maxAllowedRevision)
						break;

					this.committed.Add(@event);
					currentRevision++;
				}
			}

			this.StreamRevision = currentRevision;
			this.CommitSequence = currentSequence;
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
			var commit = new Commit(
				this.StreamId,
				this.StreamRevision + this.uncommitted.Count,
				commitId,
				this.CommitSequence + 1,
				headers,
				this.uncommitted.ToList());

			this.persistence.Commit(commit);

			this.StreamRevision = commit.StreamRevision;
			this.CommitSequence = commit.CommitSequence;
			this.uncommitted.Clear();
		}
		private void UpdateStreamOnException()
		{
			var commits = this.persistence.GetFrom(this.StreamId, this.StreamRevision + 1, int.MaxValue);
			this.PopulateStream(int.MaxValue, commits);
		}

		public void ClearChanges()
		{
			this.uncommitted.Clear();
		}
	}
}