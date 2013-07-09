namespace NEventStore
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
	/// Represents an optimistic concurrency conflict between multiple writers.
	/// </summary>
	[Serializable]
	public class ConcurrencyException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the ConcurrencyException class.
		/// </summary>
		public ConcurrencyException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the ConcurrencyException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ConcurrencyException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the ConcurrencyException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		public ConcurrencyException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the ConcurrencyException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
		protected ConcurrencyException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}