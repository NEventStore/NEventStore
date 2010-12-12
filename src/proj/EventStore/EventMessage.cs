namespace EventStore
{
	using System.Collections.Generic;

	/// <summary>
	/// Represents a single element in a stream of events.
	/// </summary>
	public class EventMessage
	{
		/// <summary>
		/// Initializes a new instance of the EventMessage class.
		/// </summary>
		public EventMessage()
		{
			this.Headers = new Dictionary<string, object>();
		}

		/// <summary>
		/// Gets the metadata which provides additional, unstructured information about this message.
		/// </summary>
		public IDictionary<string, object> Headers { get; private set; }

		/// <summary>
		/// Gets or sets the value which indicates the position of this event message within the event stream.
		/// </summary>
		public long StreamRevision { get; set; }

		/// <summary>
		/// Gets or sets the actual event message body.
		/// </summary>
		public object Body { get; set; }
	}
}