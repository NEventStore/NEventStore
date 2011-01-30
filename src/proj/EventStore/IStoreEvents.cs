namespace EventStore
{
	using System;
	using Persistence;

	/// <summary>
	/// Indicates the ability to store and retreive a stream of events.
	/// </summary>
	public interface IStoreEvents : IAccessSnapshots
	{
		/// <summary>
		/// Creates a new stream.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream to be created.</param>
		/// <returns>An empty stream.</returns>
		IEventStream CreateStream(Guid streamId);

		/// <summary>
		/// Reads the stream indicated from the minimum revision specified up to the maximum revision specified.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream from which the events will be read.</param>
		/// <param name="minRevision">The minimum revision of the stream to be read.</param>
		/// <param name="maxRevision">The maximum revision of the stream to be read.</param>
		/// <returns>A series of committed events from the stream specified.</returns>
		/// <exception cref="StorageException" />
		IEventStream OpenStream(Guid streamId, int minRevision, int maxRevision);
	}
}