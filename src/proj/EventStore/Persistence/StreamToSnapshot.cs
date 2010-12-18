namespace EventStore.Persistence
{
	using System;

	/// <summary>
	/// Indicates a stream where the last snapshot has exceeded the allowable threshold.
	/// </summary>
	public class StreamToSnapshot
	{
		/// <summary>
		/// Initializes a new instance of the StreamToSnapshot class.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream where the last snapshot exceeds the allowed threshold.</param>
		/// <param name="streamName">The name of the stream.</param>
		/// <param name="headRevision">The value which indicates the revision, length, or number of events committed to the stream.</param>
		/// <param name="snapshotRevision">The value which indicates the revision at which the last snapshot was taken.</param>
		public StreamToSnapshot(Guid streamId, string streamName, long headRevision, long snapshotRevision)
		{
			this.StreamId = streamId;
			this.StreamName = streamName;
			this.HeadRevision = headRevision;
			this.SnapshotRevision = snapshotRevision;
		}

		/// <summary>
		/// Gets the value which uniquely identifies the stream where the last snapshot exceeds the allowed threshold.
		/// </summary>
		public Guid StreamId { get; private set; }

		/// <summary>
		/// Gets the name of the stream.
		/// </summary>
		public string StreamName { get; private set; }

		/// <summary>
		/// Gets the value which indicates the revision, length, or number of events committed to the stream.
		/// </summary>
		public long HeadRevision { get; private set; }

		/// <summary>
		/// Gets the value which indicates the revision at which the last snapshot was taken.
		/// </summary>
		public long SnapshotRevision { get; private set; }
	}
}