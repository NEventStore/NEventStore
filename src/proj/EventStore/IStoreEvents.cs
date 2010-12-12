namespace EventStore
{
	using System;

	/// <summary>
	/// Indicates the ability to store and retreive a stream of events.
	/// </summary>
	public interface IStoreEvents
	{
		/// <summary>
		/// Reads the events from the stream indicated from the last snapshot, if any, up to and including the revision specified.
		/// </summary>
		/// <param name="streamId">The stream from which the events will be read.</param>
		/// <param name="maxRevision">The maximum revision of the stream to be read.</param>
		/// <returns>A stream of of committed events from the stream specified.</returns>
		CommittedEventStream ReadUntil(Guid streamId, long maxRevision);

		/// <summary>
		/// Reads the events from the stream indicated from the revision specified until the end of the stream.
		/// </summary>
		/// <param name="streamId">The stream from which the events will be read.</param>
		/// <param name="minRevision">The minimum revision of the stream to be read.</param>
		/// <returns>A stream of of committed events from the stream specified.</returns>
		CommittedEventStream ReadSince(Guid streamId, long minRevision); // this needs to track commit ids for duplicatecommitexceptions
		
		/// <summary>
		/// Writes the commit provided to the underlying storage engine.
		/// </summary>
		/// <param name="uncommitted">The set of events and associated metadata to be commited.</param>
		void Write(Commit uncommitted);
	}
}