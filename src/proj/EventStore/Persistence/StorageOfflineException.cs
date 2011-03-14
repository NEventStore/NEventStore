namespace EventStore.Persistence
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Indicates that the underlying persistence medium is unavailable or offline.
	/// </summary>
	[Serializable]
	public class StorageOfflineException : StorageException
	{
		/// <summary>
		/// Initializes a new instance of the StorageOfflineException class.
		/// </summary>
		public StorageOfflineException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the StorageOfflineException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public StorageOfflineException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the StorageOfflineException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		public StorageOfflineException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the StorageOfflineException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
		protected StorageOfflineException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}