namespace EventStore
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a series of events which have been fully committed and applied to the stream indicated.
	/// </summary>
	public class CommittedEventStream
	{
		/// <summary>
		/// Initializes a new instance of the CommittedEventStream class.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream to which the series of committed events apply.</param>
		/// <param name="streamRevision">The value indicating the most recent revision of the stream for the events retreived.</param>
		/// <param name="commitSequence">The value indicating the most recent commit applied to the stream for the events retreived.</param>
		/// <param name="events">The series of committed events.</param>
		/// <param name="commitIdentifiers">The set of values which uniquely identify the commits associated with set of events.</param>
		/// <param name="snapshot">The snapshot, if any, containing a serialized revision of the stream upon which the events provided can be applied.</param>
		public CommittedEventStream(
			Guid streamId,
			long streamRevision,
			long commitSequence,
			ICollection<object> events,
			ICollection<Guid> commitIdentifiers,
			object snapshot)
		{
			this.StreamId = streamId;
			this.StreamRevision = streamRevision;
			this.CommitSequence = commitSequence;
			this.Events = events ?? new object[] { };
			this.CommitIdentifiers = commitIdentifiers ?? new Guid[] { };
			this.Snapshot = snapshot;
		}

		/// <summary>
		/// Gets a value which uniquely identifies the stream to which the series of committed events apply.
		/// </summary>
		public Guid StreamId { get; private set; }

		/// <summary>
		/// Gets a value indicating the most recent revision of the stream for the events retreived.
		/// </summary>
		public long StreamRevision { get; private set; }

		/// <summary>
		/// Gets a value indicating the most recent commit applied to the stream for the events retreived.
		/// </summary>
		public long CommitSequence { get; private set; }

		/// <summary>
		/// Gets the series of committed events.
		/// </summary>
		public ICollection<object> Events { get; private set; }

		/// <summary>
		/// Gets the set of values which uniquely identify the commits associated with set of events.
		/// </summary>
		public ICollection<Guid> CommitIdentifiers { get; private set; }

		/// <summary>
		/// Gets the snapshot, if any, containing a serialized revision of the stream upon which the events provided can be applied.
		/// </summary>
		public object Snapshot { get; private set; }
	}
}