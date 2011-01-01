namespace EventStore.Persistence
{
	using System;

	/// <summary>
	/// Indicates the most recent information representing the head of a given stream.
	/// </summary>
	public class StreamHead
	{
		/// <summary>
		/// Initializes a new instance of the StreamHead class.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream where the last snapshot exceeds the allowed threshold.</param>
		/// <param name="streamName">The name of the stream.</param>
		/// <param name="headRevision">The value which indicates the revision, length, or number of events committed to the stream.</param>
		/// <param name="snapshotRevision">The value which indicates the revision at which the last snapshot was taken.</param>
		public StreamHead(Guid streamId, string streamName, long headRevision, long snapshotRevision)
			: this()
		{
			this.StreamId = streamId;
			this.StreamName = streamName;
			this.HeadRevision = headRevision;
			this.SnapshotRevision = snapshotRevision;
		}

		/// <summary>
		/// Initializes a new instance of the StreamHead class.
		/// </summary>
		protected StreamHead()
		{
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