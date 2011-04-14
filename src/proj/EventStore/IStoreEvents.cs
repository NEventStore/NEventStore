namespace EventStore
{
	using System;
	using Persistence;

	/// <summary>
	/// Indicates the ability to store and retreive a stream of events.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IStoreEvents : IDisposable, IAccessSnapshots
	{
		/// <summary>
		/// Creates a new stream.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream to be created.</param>
		/// <returns>An empty stream.</returns>
		IEventStream CreateStream(Guid streamId);

		/// <summary>
		/// Reads the stream indicated from the minimum revision specified up to the maximum revision specified or creates
		/// an empty stream if no commits are found and a minimum revision of zero is provided.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream from which the events will be read.</param>
		/// <param name="minRevision">The minimum revision of the stream to be read.</param>
		/// <param name="maxRevision">The maximum revision of the stream to be read.</param>
		/// <returns>A series of committed events represented as a stream.</returns>
		/// <exception cref="StorageException" />
		/// <exception cref="StorageUnavailableException" />
		/// <exception cref="StreamNotFoundException" />
		IEventStream OpenStream(Guid streamId, int minRevision, int maxRevision);

		/// <summary>
		/// Reads the stream indicated from the point of the snapshot forward until the maximum revision specified.
		/// </summary>
		/// <param name="snapshot">The snapshot of the stream to be read.</param>
		/// <param name="maxRevision">The maximum revision of the stream to be read.</param>
		/// <returns>A series of committed events represented as a stream.</returns>
		/// <exception cref="StorageException" />
		/// <exception cref="StorageUnavailableException" />
		IEventStream OpenStream(Snapshot snapshot, int maxRevision);
	}
}