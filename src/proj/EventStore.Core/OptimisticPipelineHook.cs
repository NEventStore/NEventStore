namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using Persistence;

	/// <summary>
	/// Tracks the heads of streams to reduce latency by avoiding roundtrips to storage.
	/// </summary>
	public class OptimisticPipelineHook : IPipelineHook
	{
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
			this.maxStreamsToTrack = maxStreamsToTrack;
		}

		public virtual Commit Select(Commit committed)
		{
			this.Track(committed);
			return committed;
		}
		public virtual bool PreCommit(Commit attempt)
		{
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
			this.maxItemsToTrack.AddFirst(committed.StreamId);
			if (this.maxItemsToTrack.Count <= this.maxStreamsToTrack)
				return;

			this.heads.Remove(this.maxItemsToTrack.Last.Value);
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