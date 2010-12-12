namespace EventStore
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a series of events committed as a single unit which apply to the stream indicated.
	/// </summary>
	public class Commit
	{
		/// <summary>
		/// Initializes a new instance of the Commit class.
		/// </summary>
		public Commit()
		{
			this.Headers = new Dictionary<string, object>();
			this.Events = new LinkedList<EventMessage>();
		}

		/// <summary>
		/// Gets or sets the value which uniquely identifies the stream to which the commit belongs.
		/// </summary>
		public Guid StreamId { get; set; }

		/// <summary>
		/// Gets or sets the friendly name of the stream.
		/// </summary>
		public string StreamName { get; set; }

		/// <summary>
		/// Gets or sets the value which uniquely identifies the commit within the stream.
		/// </summary>
		public Guid CommitId { get; set; }

		/// <summary>
		/// Gets or sets the value which indicates the sequence (or position) in the stream to which this commit applies.
		/// </summary>
		public long CommitSequence { get; set; }

		/// <summary>
		/// Gets the metadata which provides additional, unstructured information about this commit.
		/// </summary>
		public IDictionary<string, object> Headers { get; private set; }

		/// <summary>
		/// Gets the collection of event messages to be committed as a single unit.
		/// </summary>
		public ICollection<EventMessage> Events { get; private set; }
	}
}