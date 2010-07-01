namespace EventStore
{
	using System;
	using System.Collections;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents an attempt to commit events resulting from the same command more than once.
	/// </summary>
	[Serializable]
	public class DuplicateCommandException : Exception
	{
		/// <summary>
		/// Gets the collection of events previously committed by the associated command.
		/// </summary>
		public ICollection CommittedEvents { get; private set; }

		/// <summary>
		/// Initializes a new instance of the DuplicateCommandException class.
		/// </summary>
		public DuplicateCommandException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the DuplicateCommandException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public DuplicateCommandException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the DuplicateCommandException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		public DuplicateCommandException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the DuplicateCommandException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		/// <param name="committedEvents">The collection of events previously committed by handled the associated command.</param>
		public DuplicateCommandException(string message, Exception innerException, ICollection committedEvents)
			: this(message, innerException)
		{
			this.CommittedEvents = committedEvents;
		}

		/// <summary>
		/// Initializes a new instance of the DuplicateCommandException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
		protected DuplicateCommandException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}