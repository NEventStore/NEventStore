namespace EventStore
{
	using System;
	using System.Collections;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents an attempt to handle the same command.
	/// </summary>
	[Serializable]
	public class AlreadyHandledCommandException : Exception
	{
		/// <summary>
		/// Gets the collection of events previously committed by the associated command.
		/// </summary>
		public ICollection CommittedEvents { get; private set; }

		/// <summary>
		/// Initializes a new instance of the AlreadyHandledCommandException class.
		/// </summary>
		public AlreadyHandledCommandException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the AlreadyHandledCommandException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public AlreadyHandledCommandException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the AlreadyHandledCommandException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		public AlreadyHandledCommandException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the AlreadyHandledCommandException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		/// <param name="committedEvents">The collection of events previously committed by handled the associated command.</param>
		public AlreadyHandledCommandException(string message, Exception innerException, ICollection committedEvents)
			: this(message, innerException)
		{
			this.CommittedEvents = committedEvents;
		}

		/// <summary>
		/// Initializes a new instance of the AlreadyHandledCommandException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
		protected AlreadyHandledCommandException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}