namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using Persistence;

	/// <summary>
	/// Represents a series of events to be committed as a single unit and which apply to the stream indicated.
	/// </summary>
	public class CommitAttempt
	{
		/// <summary>
		/// Initializes a new instance of the CommitAttempt class.
		/// </summary>
		public CommitAttempt()
		{
			this.Headers = new Dictionary<string, object>();
			this.Events = new List<EventMessage>();
		}

		/// <summary>
		/// Gets or sets the value which uniquely identifies the stream to which the commit attempt belongs.
		/// </summary>
		public virtual Guid StreamId { get; set; }

		/// <summary>
		/// Gets or sets the value which indicates the new head revision of the stream to which this commit attempt applies.
		/// </summary>
		public virtual int StreamRevision { get; set; }

		/// <summary>
		/// Gets or sets the value which uniquely identifies the commit within the stream.
		/// </summary>
		public virtual Guid CommitId { get; set; }

		/// <summary>
		/// Gets or sets the value which indicates the most recent, known committed sequence for the stream to which this commit attempt applies.
		/// </summary>
		public virtual int PreviousCommitSequence { get; set; }

		/// <summary>
		/// Gets the metadata which provides additional, unstructured information about this commit attempt.
		/// </summary>
		public virtual Dictionary<string, object> Headers { get; private set; }

		/// <summary>
		/// Gets the collection of event messages to be committed as a single unit.
		/// </summary>
		public virtual List<EventMessage> Events { get; private set; }

		/// <summary>
		/// Converts the attempt into a commit.
		/// </summary>
		/// <returns>A fully populated object instance of the <see cref="Commit"/> class.</returns>
		public virtual Commit ToCommit()
		{
			return new Commit(
				this.StreamId,
				this.StreamRevision,
				this.CommitId,
				this.PreviousCommitSequence + 1,
				this.Headers,
				this.Events,
				null);
		}
	}
}