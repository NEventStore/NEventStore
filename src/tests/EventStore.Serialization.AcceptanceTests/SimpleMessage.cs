namespace EventStore.Serialization.AcceptanceTests
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;

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

		[SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists",
			Justification = "This is an acceptance test DTO and the structure doesn't really matter.")]
		public List<string> Contents { get; private set; }
	}
}