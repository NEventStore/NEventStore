namespace EventStore
{
	using System;

	/// <summary>
	/// Indicates the ability to store and retreive a stream of events.
	/// </summary>
	public interface IStoreEvents
	{
		/// <summary>
		/// Reads from the stream indicated from most recent snapshot, if any, up to and including the revision specified.
		/// </summary>
		/// <param name="streamId">The stream from which the events will be read.</param>
		/// <param name="maxRevision">The maximum revision of the stream to be read.</param>
		/// <returns>A series of committed events from the stream specified.</returns>
		CommittedEventStream ReadUntil(Guid streamId, long maxRevision);

		/// <summary>
		/// Reads from the stream indicated from the revision specified until the end of the stream.
		/// </summary>
		/// <param name="streamId">The stream from which the events will be read.</param>
		/// <param name="minRevision">The minimum revision of the stream to be read.</param>
		/// <returns>A series of committed events from the stream specified.</returns>
		CommittedEventStream ReadFrom(Guid streamId, long minRevision); // TODO: this needs to track commit ids for duplicatecommitexceptions
		
		/// <summary>
		/// Writes the to-be-commited events provided to the underlying storage infrastructure.
		/// </summary>
		/// <param name="uncommitted">The series of events and associated metadata to be commited.</param>
		void Write(CommitAttempt uncommitted);
	}
}