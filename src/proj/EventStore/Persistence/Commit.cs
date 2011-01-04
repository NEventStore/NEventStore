namespace EventStore.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents a series of events which have been fully committed as a single unit and which apply to the stream indicated.
	/// </summary>
	[DataContract]
	[Serializable]
	public class Commit
	{
		/// <summary>
		/// Initializes a new instance of the Commit class.
		/// </summary>
		/// <param name="streamId">The value which uniquely identifies the stream to which the commit belongs.</param>
		/// <param name="commitId">The value which uniquely identifies the commit within the stream.</param>
		/// <param name="streamRevision">The value which indicates the revision of the most recent event in the stream to which this commit applies.</param>
		/// <param name="commitSequence">The value which indicates the sequence (or position) in the stream to which this commit applies.</param>
		/// <param name="headers">The metadata which provides additional, unstructured information about this commit.</param>
		/// <param name="events">The collection of event messages to be committed as a single unit.</param>
		/// <param name="snapshot">The snapshot, if any, which represents a materialization of the stream at the last event of the commit.</param>
		public Commit(
			Guid streamId,
			Guid commitId,
			long streamRevision,
			long commitSequence,
			IDictionary<string, object> headers,
			ICollection<EventMessage> events,
			object snapshot)
		{
			this.StreamId = streamId;
			this.CommitId = commitId;
			this.StreamRevision = streamRevision;
			this.CommitSequence = commitSequence;
			this.Headers = headers ?? new Dictionary<string, object>();
			this.Events = events ?? new LinkedList<EventMessage>();
			this.Snapshot = snapshot;
		}

		/// <summary>
		/// Gets the value which uniquely identifies the stream to which the commit belongs.
		/// </summary>
		[DataMember] public virtual Guid StreamId { get; private set; }

		/// <summary>
		/// Gets the value which uniquely identifies the commit within the stream.
		/// </summary>
		[DataMember] public virtual Guid CommitId { get; private set; }

		/// <summary>
		/// Gets the value which indicates the revision of the most recent event in the stream to which this commit applies.
		/// </summary>
		[DataMember] public virtual long StreamRevision { get; private set; }

		/// <summary>
		/// Gets the value which indicates the sequence (or position) in the stream to which this commit applies.
		/// </summary>
		[DataMember] public virtual long CommitSequence { get; private set; }

		/// <summary>
		/// Gets the metadata which provides additional, unstructured information about this commit.
		/// </summary>
		[DataMember] public virtual IDictionary<string, object> Headers { get; private set; }

		/// <summary>
		/// Gets the collection of event messages to be committed as a single unit.
		/// </summary>
		[DataMember] public virtual ICollection<EventMessage> Events { get; private set; }

		/// <summary>
		/// Gets the snapshot, if any, which represents a materialization of the stream at the last event of the commit.
		/// </summary>
		[DataMember] public object Snapshot { get; private set; }

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>If the two objects are equal, returns true; otherwise false.</returns>
		public override bool Equals(object obj)
		{
			var commit = obj as Commit;
			return commit != null && commit.StreamId == this.StreamId && commit.CommitId == this.CommitId;
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>The hash code for this instance.</returns>
		public override int GetHashCode()
		{
			return this.StreamId.GetHashCode() ^ this.CommitId.GetHashCode();
		}
	}
}