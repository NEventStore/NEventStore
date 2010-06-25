namespace EventStore
{
	using System;
	using System.Runtime.Serialization;

	public class EventStoreException : Exception
	{
		public EventStoreException()
		{
		}

		public EventStoreException(string message)
			: base(message)
		{
		}

		public EventStoreException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected EventStoreException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}