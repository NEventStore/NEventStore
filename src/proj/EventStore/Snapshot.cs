namespace EventStore
{
	using System;

	/// <summary>
	/// Represents a materialized view of a stream at specific revision.
	/// </summary>
	public class Snapshot
	{
		/// <summary>
		/// Initializes a new instance of the Snapshot class.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream to which the snapshot applies.</param>
		/// <param name="streamRevision">The position at which the snapshot applies.</param>
		/// <param name="payload">The snapshot or materialized view of the stream at the revision indicated.</param>
		public Snapshot(Guid streamId, int streamRevision, object payload)
			: this()
		{
			this.StreamId = streamId;
			this.StreamRevision = streamRevision;
			this.Payload = payload;
		}

		/// <summary>
		/// Initializes a new instance of the Snapshot class.
		/// </summary>
		protected Snapshot()
		{
		}

		/// <summary>
		/// Gets the value which uniquely identifies the stream to which the snapshot applies.
		/// </summary>
		public Guid StreamId { get; private set; }

		/// <summary>
		/// Gets the position at which the snapshot applies.
		/// </summary>
		public int StreamRevision { get; private set; }

		/// <summary>
		/// Gets the snapshot or materialized view of the stream at the revision indicated.
		/// </summary>
		public object Payload { get; private set; }
	}
}