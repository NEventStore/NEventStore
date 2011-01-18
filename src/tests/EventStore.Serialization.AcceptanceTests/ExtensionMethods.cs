namespace EventStore.Serialization.AcceptanceTests
{
	using System;
	using Persistence;

	internal static class ExtensionMethods
	{
		public static SimpleMessage Populate(this SimpleMessage message)
		{
			return new SimpleMessage
			{
				Id = Guid.NewGuid(),
				Count = 1234,
				Created = new DateTime(2000, 2, 3, 4, 5, 6, 7),
				Value = "Hello, World!",
				Contents = { "a", null, string.Empty, "d" }
			};
		}

		public static Commit Populate(this CommitAttempt attempt)
		{
			attempt = new CommitAttempt
			{
				StreamId = Guid.NewGuid(),
				CommitId = Guid.NewGuid(),
				PreviousCommitSequence = 1,
				StreamRevision = 2,
				Headers = { { "Key", "Value" }, { "Key2", (long)1234 }, { "Key3", null } },
				Events =
				{
					new EventMessage
					{
						Headers =
						{
							{ "MsgKey1", TimeSpan.MinValue },
							{ "MsgKey2", Guid.NewGuid() },
							{ "MsgKey3", 1.1M },
							{ "MsgKey4", (ushort)1 }
						},
						Body = "some value"
					}
				}
			};

			return attempt.ToCommit();
		}
	}
}