namespace CommonDomain.Persistence
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	///   Represents a command that could not be executed because it conflicted with the command of another user or actor.
	/// </summary>
	[Serializable]
	public class ConflictingCommandException : Exception
	{
		/// <summary>
		///   Initializes a new instance of the ConflictingCommandException class.
		/// </summary>
		public ConflictingCommandException()
		{}

		/// <summary>
		///   Initializes a new instance of the ConflictingCommandException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ConflictingCommandException(string message)
			: base(message)
		{}

		/// <summary>
		///   Initializes a new instance of the ConflictingCommandException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		public ConflictingCommandException(string message, Exception innerException)
			: base(message, innerException)
		{}

		/// <summary>
		///   Initializes a new instance of the ConflictingCommandException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
		protected ConflictingCommandException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{}
	}
}