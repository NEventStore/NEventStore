namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	/// <summary>
	/// Represents an optimistic concurrency conflict between multiple writers.
	/// </summary>
	[Serializable]
	public class ConcurrencyException : Exception
	{
		private readonly ICollection<Commit> commits = new Commit[0];

		/// <summary>
		/// Gets the collection of commits that caused the concurrency exception to occur.
		/// </summary>
		public ICollection<Commit> Commits
		{
			get { return this.commits; }
		}

		/// <summary>
		/// Initializes a new instance of the ConcurrencyException class.
		/// </summary>
		public ConcurrencyException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the ConcurrencyException class.
		/// </summary>
		/// <param name="commits">The commits discovered from the concurrency exception.</param>
		public ConcurrencyException(IEnumerable<Commit> commits)
		{
			this.commits = (commits ?? this.commits).ToArray();
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