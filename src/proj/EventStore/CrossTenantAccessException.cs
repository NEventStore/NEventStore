namespace EventStore
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents an unauthorized attempt to reach across multi-tenant paritioning boundaries.
	/// </summary>
	[Serializable]
	public class CrossTenantAccessException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the CrossTenantAccessException class.
		/// </summary>
		public CrossTenantAccessException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the CrossTenantAccessException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public CrossTenantAccessException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the CrossTenantAccessException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The message that is the cause of the current exception.</param>
		public CrossTenantAccessException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the CrossTenantAccessException class.
		/// </summary>
		/// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
		/// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
		protected CrossTenantAccessException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}