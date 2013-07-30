namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Persistence;

    /// <summary>
	/// Tracks the heads of streams to reduce latency by avoiding roundtrips to storage.
	/// </summary>
	public class OptimisticPipelineHook : IPipelineHook
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(OptimisticPipelineHook));
		private const int MaxStreamsToTrack = 100;
		private readonly LinkedList<Guid> maxItemsToTrack = new LinkedList<Guid>();
		private readonly IDictionary<Guid, Commit> heads = new Dictionary<Guid, Commit>();
		private readonly int maxStreamsToTrack;

		public OptimisticPipelineHook()
			: this(MaxStreamsToTrack)
		{
		}
		public OptimisticPipelineHook(int maxStreamsToTrack)
		{
			Logger.Debug(Resources.TrackingStreams, maxStreamsToTrack);
			this.maxStreamsToTrack = maxStreamsToTrack;
		}
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			this.heads.Clear();
			this.maxItemsToTrack.Clear();
		}

		public virtual Commit Select(Commit committed)
		{
			this.Track(committed);
			return committed;
		}
		public virtual bool PreCommit(Commit attempt)
		{
			Logger.Debug(Resources.OptimisticConcurrencyCheck, attempt.StreamId);

			var head = this.GetStreamHead(attempt.StreamId);
			if (head == null)
				return true;

			if (head.CommitSequence >= attempt.CommitSequence)
				throw new ConcurrencyException();

			if (head.StreamRevision >= attempt.StreamRevision)
				throw new ConcurrencyException();

			if (head.CommitSequence < attempt.CommitSequence - 1)
				throw new StorageException(); // beyond the end of the stream

			if (head.StreamRevision < attempt.StreamRevision - attempt.Events.Count)
				throw new StorageException(); // beyond the end of the stream

			Logger.Debug(Resources.NoConflicts, attempt.StreamId);
			return true;
		}
		public virtual void PostCommit(Commit committed)
		{
			this.Track(committed);
		}

		public virtual void Track(Commit committed)
		{
			if (committed == null)
				return;

			lock (this.maxItemsToTrack)
			{
				this.UpdateStreamHead(committed);
				this.TrackUpToCapacity(committed);
			}
		}
		private void UpdateStreamHead(Commit committed)
		{
			var head = this.GetStreamHead(committed.StreamId);
			if (AlreadyTracked(head))
				this.maxItemsToTrack.Remove(committed.StreamId);

			head = head ?? committed;
			head = head.StreamRevision > committed.StreamRevision ? head : committed;

			this.heads[committed.StreamId] = head;
		}
		private static bool AlreadyTracked(Commit head)
		{
			return head != null;
		}
		private void TrackUpToCapacity(Commit committed)
		{
			Logger.Verbose(Resources.TrackingCommit, committed.CommitSequence, committed.StreamId);
			this.maxItemsToTrack.AddFirst(committed.StreamId);
			if (this.maxItemsToTrack.Count <= this.maxStreamsToTrack)
				return;

			var expired = this.maxItemsToTrack.Last.Value;
			Logger.Verbose(Resources.NoLongerTrackingStream, expired);

			this.heads.Remove(expired);
			this.maxItemsToTrack.RemoveLast();
		}

		public virtual bool Contains(Commit attempt)
		{
			return this.GetStreamHead(attempt.StreamId) != null;
		}

		private Commit GetStreamHead(Guid streamId)
		{
			lock (this.maxItemsToTrack)
			{
				Commit head;
				this.heads.TryGetValue(streamId, out head);
				return head;
			}
		}
	}
}