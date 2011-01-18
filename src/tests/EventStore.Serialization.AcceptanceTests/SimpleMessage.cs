namespace EventStore.Serialization.AcceptanceTests
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public class SimpleMessage
	{
		public SimpleMessage()
		{
			this.Contents = new List<string>();
		}

		public Guid Id { get; set; }
		public DateTime Created { get; set; }
		public string Value { get; set; }
		public int Count { get; set; }
		public List<string> Contents { get; private set; }
	}
}