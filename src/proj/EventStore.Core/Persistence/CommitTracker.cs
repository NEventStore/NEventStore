namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Tracks the commits for a set of streams to determine if a particular commit has already
	/// been committed thus relaxing the requirements upon the persistence engine as well as
	/// reducing latency by avoiding needless database roundtrips through keeping the values which
	/// uniquely identify each commit in memory.
	/// </summary>
	/// <remarks>
	/// For storage engines with relaxed consistency guarantees, such as a document database,
	/// the CommitTracker prevents the need to query the persistence engine prior to a commit.
	/// For storage engines with stronger consistency guarantees, such as a relational database,
	/// the CommitTracker helps to avoid the increased latency incurred from extra roundtrips.
	/// </remarks>
	public class CommitTracker
	{
		private const int MaxCommitsTrackedPerStream = 1000;
		private readonly IDictionary<Guid, TrackedStream> streams = new Dictionary<Guid, TrackedStream>();
		private readonly int commitsToTrackPerStream;

		public CommitTracker()
			: this(MaxCommitsTrackedPerStream)
		{
		}
		public CommitTracker(int commitsToTrackPerStream)
		{
			this.commitsToTrackPerStream = commitsToTrackPerStream;
		}

		public virtual void Track(Commit committed)
		{
			TrackedStream stream;

			lock (this.streams)
			{
				if (!this.streams.TryGetValue(committed.StreamId, out stream))
					this.streams[committed.StreamId] = stream = new TrackedStream(this.commitsToTrackPerStream);
			}

			stream.Track(committed.CommitId);
		}
		public virtual bool Contains(CommitAttempt attempt)
		{
			lock (this.streams)
			{
				TrackedStream stream;
				return this.streams.TryGetValue(attempt.StreamId, out stream)
				       && stream.Contains(attempt.CommitId);
			}
		}

		private class TrackedStream
		{
			private readonly ICollection<Guid> lookup = new HashSet<Guid>();
			private readonly LinkedList<Guid> ordered = new LinkedList<Guid>();
			private readonly int commitsToTrack;

			public TrackedStream(int commitsToTrack)
			{
				this.commitsToTrack = commitsToTrack;
			}

			public void Track(Guid commitId)
			{
				if (this.lookup.Contains(commitId))
					return;

				lock (this.lookup)
				{
					if (this.lookup.Contains(commitId))
						return;

					this.lookup.Add(commitId);
					this.ordered.AddLast(commitId);

					if (this.ordered.Count <= this.commitsToTrack)
						return;

					var commitIdToRemove = this.ordered.First;
					this.ordered.RemoveFirst();
					this.lookup.Remove(commitIdToRemove.Value);
				}
			}
			public bool Contains(Guid commitId)
			{
				return this.lookup.Contains(commitId);
			}
		}
	}
}