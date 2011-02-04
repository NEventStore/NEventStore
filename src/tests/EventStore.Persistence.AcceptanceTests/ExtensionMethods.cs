namespace EventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Collections.Generic;

	internal static class ExtensionMethods
	{
		public static Commit BuildAttempt(this Guid streamId, DateTime now)
		{
			var messages = new List<EventMessage>
			{
				new EventMessage { Body = new SomeDomainEvent { SomeProperty = "Test" } },
				new EventMessage { Body = new SomeDomainEvent { SomeProperty = "Test2" } },
			};

			return new Commit(
				streamId,
				2,
				Guid.NewGuid(),
				1,
				now,
				new Dictionary<string, object> { { "A header", "A string value" }, { "Another header", 2 } },
				messages);
		}
		public static Commit BuildAttempt(this Guid streamId)
		{
			return streamId.BuildAttempt(DateTime.UtcNow);
		}
		public static Commit BuildNextAttempt(this Commit commit)
		{
			var messages = new List<EventMessage>
			{
				new EventMessage { Body = new SomeDomainEvent { SomeProperty = "Another test" } },
				new EventMessage { Body = new SomeDomainEvent { SomeProperty = "Another test2" } },
			};

			return new Commit(
				commit.StreamId,
				commit.StreamRevision + 2,
				Guid.NewGuid(),
				commit.CommitSequence + 1,
				commit.CommitStamp,
				new Dictionary<string, object>(),
				messages);
		}

		[Serializable]
		public class SomeDomainEvent
		{
			public string SomeProperty { get; set; }
		}
	}
}