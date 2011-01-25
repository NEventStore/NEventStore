namespace EventStore.Persistence.AcceptanceTests
{
	using System;

	internal static class ExtensionMethods
	{
		public static CommitAttempt BuildAttempt(this Guid streamId)
		{
			return new CommitAttempt
			{
				StreamId = streamId,
				CommitId = Guid.NewGuid(),
				PreviousCommitSequence = 0,
				StreamRevision = 2,
				Headers = {{"A header","A string value"},{"Another header",2}},
				Events =
				{
					new EventMessage
						{
							Body = new SomeDomainEvent{SomeProperty = "Test"}	
						},
					new EventMessage
						{
							Body = new SomeDomainEvent{SomeProperty = "Test2"}	
						},
				}
			};
		}
		public static CommitAttempt BuildNextAttempt(this CommitAttempt successful)
		{
			var commit = successful.ToCommit();
			return new CommitAttempt
			{
				StreamId = commit.StreamId,
				CommitId = Guid.NewGuid(),
				PreviousCommitSequence = commit.CommitSequence,
				StreamRevision = commit.StreamRevision + 2,
				Events =
				{
					new EventMessage
						{
							Body = new SomeDomainEvent{SomeProperty = "Another test"}	
						},
					new EventMessage
						{
							Body = new SomeDomainEvent{SomeProperty = "Another test2"}	
						},
				}
			};
		}

		[Serializable]
		public class SomeDomainEvent
		{
			public string SomeProperty { get; set; }
		}
	}
}