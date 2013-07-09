namespace EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Logging;

    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
		Justification = "This behaves like a stream--not a .NET 'Stream' object, but a stream nonetheless.")]
	public class OptimisticEventStream : IEventStream
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(OptimisticEventStream));
		private readonly ICollection<EventMessage> committed = new LinkedList<EventMessage>();
		private readonly ICollection<EventMessage> events = new LinkedList<EventMessage>();
		private readonly IDictionary<string, object> uncommittedHeaders = new Dictionary<string, object>();
		private readonly IDictionary<string, object> committedHeaders = new Dictionary<string, object>();
		private readonly ICollection<Guid> identifiers = new HashSet<Guid>();
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
				Logger.Verbose(Resources.AddingCommitsToStream, commit.CommitId, commit.Events.Count, this.StreamId);
				this.identifiers.Add(commit.CommitId);

				this.CommitSequence = commit.CommitSequence;
				var currentRevision = commit.StreamRevision - commit.Events.Count + 1;
				if (currentRevision > maxRevision)
					return;

				this.CopyToCommittedHeaders(commit);
				this.CopyToEvents(minRevision, maxRevision, currentRevision, commit);
			}
		}
		private void CopyToCommittedHeaders(Commit commit)
		{
			foreach (var key in commit.Headers.Keys)
				this.committedHeaders[key] = commit.Headers[key];
		}
		private void CopyToEvents(int minRevision, int maxRevision, int currentRevision, Commit commit)
		{
			foreach (var @event in commit.Events)
			{
				if (currentRevision > maxRevision)
				{
					Logger.Debug(Resources.IgnoringBeyondRevision, commit.CommitId, this.StreamId, maxRevision);
					break;
				}

				if (currentRevision++ < minRevision)
				{
					Logger.Debug(Resources.IgnoringBeforeRevision, commit.CommitId, this.StreamId, maxRevision);
					continue;
				}

				this.committed.Add(@event);
				this.StreamRevision = currentRevision - 1;
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
		public virtual IDictionary<string, object> CommittedHeaders
		{
			get { return this.committedHeaders; }
		}

		public virtual ICollection<EventMessage> UncommittedEvents
		{
			get { return new ImmutableCollection<EventMessage>(this.events); }
		}
		public virtual IDictionary<string, object> UncommittedHeaders
		{
			get { return this.uncommittedHeaders; }
		}

		public virtual void Add(EventMessage uncommittedEvent)
		{
			if (uncommittedEvent == null || uncommittedEvent.Body == null)
				return;

			Logger.Debug(Resources.AppendingUncommittedToStream, this.StreamId);
			this.events.Add(uncommittedEvent);
		}

		public virtual void CommitChanges(Guid commitId)
		{
			Logger.Debug(Resources.AttemptingToCommitChanges, this.StreamId);

			if (this.identifiers.Contains(commitId))
				throw new DuplicateCommitException();

			if (!this.HasChanges())
				return;

			try
			{
				this.PersistChanges(commitId);
			}
			catch (ConcurrencyException)
			{
				Logger.Info(Resources.UnderlyingStreamHasChanged, this.StreamId);
				var commits = this.persistence.GetFrom(this.StreamId, this.StreamRevision + 1, int.MaxValue);
				this.PopulateStream(this.StreamRevision + 1, int.MaxValue, commits);

				throw;
			}
		}
		protected virtual bool HasChanges()
		{
			if (this.disposed)
				throw new ObjectDisposedException(Resources.AlreadyDisposed);

			if (this.events.Count > 0)
				return true;

			Logger.Warn(Resources.NoChangesToCommit, this.StreamId);
			return false;
		}
		protected virtual void PersistChanges(Guid commitId)
		{
			var attempt = this.BuildCommitAttempt(commitId);

			Logger.Debug(Resources.PersistingCommit, commitId, this.StreamId);
			this.persistence.Commit(attempt);

			this.PopulateStream(this.StreamRevision + 1, attempt.StreamRevision, new[] { attempt });
			this.ClearChanges();
		}
		protected virtual Commit BuildCommitAttempt(Guid commitId)
		{
			Logger.Debug(Resources.BuildingCommitAttempt, commitId, this.StreamId);
			return new Commit(
				this.StreamId,
				this.StreamRevision + this.events.Count,
				commitId,
				this.CommitSequence + 1,
				SystemTime.UtcNow,
				this.uncommittedHeaders.ToDictionary(x => x.Key, x => x.Value),
				this.events.ToList());
		}

		public virtual void ClearChanges()
		{
			Logger.Debug(Resources.ClearingUncommittedChanges, this.StreamId);
			this.events.Clear();
			this.uncommittedHeaders.Clear();
		}
	}
}