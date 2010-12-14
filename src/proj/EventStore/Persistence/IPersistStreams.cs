namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;
	using Dispatcher;

	/// <summary>
	/// Indicates the ability to adapt the underlying persistence infrastructure to behave like a stream of events.
	/// </summary>
	public interface IPersistStreams : ITrackDispatchedCommits
	{
		/// <summary>
		/// Gets the corresponding commits from the stream indicated starting at the most recent snapshot, if any,
		/// up to and including the revision specified sorted in ascending order--from oldest to newest.
		/// </summary>
		/// <param name="streamId">The stream from which the events will be read.</param>
		/// <param name="maxRevision">The maximum revision of the stream to be read.</param>
		/// <returns>A series of committed events from the stream specified sorted in ascending order.</returns>
		IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision);

		/// <summary>
		/// Gets the corresponding commits from the stream indicated starting at the revision specified until the
		/// end of the stream sorted in ascending order--from oldest to newest.
		/// </summary>
		/// <param name="streamId">The stream from which the events will be read.</param>
		/// <param name="minRevision">The minimum revision of the stream to be read.</param>
		/// <returns>A series of committed events from the stream specified sorted in ascending order..</returns>
		IEnumerable<Commit> GetFrom(Guid streamId, long minRevision);

		/// <summary>
		/// Writes the to-be-commited events provided to the underlying persistence mechanism.
		/// </summary>
		/// <param name="uncommitted">The series of events and associated metadata to be commited.</param>
		void Persist(CommitAttempt uncommitted);

		/// <summary>
		/// Gets identifiers for all streams whose head and last snapshot revisions differ by at least the threshold specified.
		/// </summary>
		/// <param name="maxThreshold">The maximum difference between the head and most recent snapshot revisions.</param>
		/// <returns>The streams for which the head and snapshot revisions differ by at least the threshold specified.</returns>
		IEnumerable<Guid> GetStreamsToSnapshot(int maxThreshold);

		/// <summary>
		/// Adds the snapshot provided to the stream indicated the commit sequence specified.
		/// </summary>
		/// <param name="streamId">The stream to which the snapshot provided should be added.</param>
		/// <param name="commitSequence">The sequence in the series of commits at which the snapshot should be added.</param>
		/// <param name="snapshot">The snapshot or materialized view of the stream.</param>
		void AddSnapshot(Guid streamId, long commitSequence, object snapshot);
	}
}