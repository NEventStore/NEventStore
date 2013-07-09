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
		/// <param name="headRevision">The value which indicates the revision, length, or number of events committed to the stream.</param>
		/// <param name="snapshotRevision">The value which indicates the revision at which the last snapshot was taken.</param>
		public StreamHead(Guid streamId, int headRevision, int snapshotRevision)
			: this()
		{
			this.StreamId = streamId;
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
		/// Gets the value which indicates the revision, length, or number of events committed to the stream.
		/// </summary>
		public int HeadRevision { get; private set; }

		/// <summary>
		/// Gets the value which indicates the revision at which the last snapshot was taken.
		/// </summary>
		public int SnapshotRevision { get; private set; }

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>If the two objects are equal, returns true; otherwise false.</returns>
		public override bool Equals(object obj)
		{
			var commit = obj as StreamHead;
			return commit != null && commit.StreamId == this.StreamId;
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>The hash code for this instance.</returns>
		public override int GetHashCode()
		{
			return this.StreamId.GetHashCode();
		}
	}
}