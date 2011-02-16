namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using Persistence;

	/// <summary>
	/// Indicates the ability to track a series of events and commit them to durable storage.
	/// </summary>
	/// <remarks>
	/// This class is single threaded and should not be shared between threads.
	/// </remarks>
	public interface IEventStream : IDisposable
	{
		/// <summary>
		/// Gets the value which uniquely identifies the stream to which the stream belongs.
		/// </summary>
		Guid StreamId { get; }

		/// <summary>
		/// Gets the value which indiciates the most recent committed revision of event stream.
		/// </summary>
		int StreamRevision { get; }

		/// <summary>
		/// Gets the value which indicates the most recent committed sequence identifier of the event stream.
		/// </summary>
		int CommitSequence { get; }

		/// <summary>
		/// Gets the collection of events which have been successfully persisted to durable storage.
		/// </summary>
		ICollection<EventMessage> CommittedEvents { get; }

		/// <summary>
		/// Gets the collection of yet-to-be-committed events that have not yet been persisted to durable storage.
		/// </summary>
		ICollection<EventMessage> UncommittedEvents { get; }

		/// <summary>
		/// Gets the collection of yet-to-be-committed headers associated with the uncommitted events.
		/// </summary>
		IDictionary<string, object> UncommittedHeaders { get; }

		/// <summary>
		/// Adds the event messages provided to the session to be tracked.
		/// </summary>
		/// <param name="uncommittedEvents">The events to be tracked.</param>
		void Add(params EventMessage[] uncommittedEvents);

		/// <summary>
		/// Adds the event messages provided to the session to be tracked.
		/// </summary>
		/// <param name="uncommittedEvents">The events to be tracked.</param>
		void Add(IEnumerable<EventMessage> uncommittedEvents);

		/// <summary>
		/// Commits the changes to durable storage.
		/// </summary>
		/// <param name="commitId">The value which uniquely identifies the commit.</param>
		/// <exception cref="DuplicateCommitException" />
		/// <exception cref="ConcurrencyException" />
		/// <exception cref="StorageException" />
		void CommitChanges(Guid commitId);

		/// <summary>
		/// Clears the uncommitted changes.
		/// </summary>
		void ClearChanges();
	}
}