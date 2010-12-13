namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;
	using Dispatcher;

	/// <summary>
	/// Indicates the ability to adapt the underlying persistence infrastructure to behave like a stream of events.
	/// </summary>
	public interface IPersistStreams : ITrackDispatchedEvents
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
	}
}