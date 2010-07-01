namespace EventStore
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents a failed attempt to insert a duplicate key into the storage engine.
	/// </summary>
	[Serializable]
	public class DuplicateKeyException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the DuplicateKeyException class.
		/// </summary>
		public DuplicateKeyException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the DuplicateKeyException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public DuplicateKeyException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the DuplicateKeyException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		public DuplicateKeyException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the DuplicateKeyException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
		protected DuplicateKeyException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}