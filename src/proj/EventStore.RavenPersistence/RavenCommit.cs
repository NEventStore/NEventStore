namespace EventStore.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using Persistence;

	public class RavenCommit
	{
		private const string KeyFormat = "commits/{0}.{1}";

		public RavenCommit()
		{
		}
		public RavenCommit(CommitAttempt attempt)
			: this(attempt.ToCommit())
		{
		}
		public RavenCommit(Commit commit)
		{
			this.StreamId = commit.StreamId;
			this.CommitId = commit.CommitId;
			this.StreamRevision = commit.StreamRevision;
			this.CommitSequence = commit.CommitSequence;
			this.Headers = commit.Headers;
			this.Events = commit.Events;
			this.Snapshot = commit.Snapshot;
			this.PendingDispatch = true;
		}

		public string Id
		{
			get { return KeyFormat.FormatWith(this.StreamId, this.CommitSequence); }
		}

		public Guid StreamId { get; set; }
		public Guid CommitId { get; set; }
		public long StreamRevision { get; set; }
		public long CommitSequence { get; set; }
		public IDictionary<string, object> Headers { get; set; }
		public ICollection<EventMessage> Events { get; set; }
		public object Snapshot { get; set; }

		public bool PendingDispatch { get; set; }

		public Commit ToCommit()
		{
			return new Commit(
				this.StreamId,
				this.CommitId,
				this.StreamRevision,
				this.CommitSequence,
				this.Headers,
				this.Events,
				this.Snapshot);
		}
	}
}