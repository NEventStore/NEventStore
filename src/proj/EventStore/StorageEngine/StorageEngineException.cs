namespace EventStore.StorageEngine
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents a general failure of the storage engine.
	/// </summary>
	[Serializable]
	public class StorageEngineException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the StorageEngineException class.
		/// </summary>
		public StorageEngineException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the StorageEngineException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public StorageEngineException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the StorageEngineException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		public StorageEngineException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the StorageEngineException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
		protected StorageEngineException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}