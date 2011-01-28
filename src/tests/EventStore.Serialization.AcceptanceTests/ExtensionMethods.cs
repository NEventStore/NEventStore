namespace EventStore.Serialization.AcceptanceTests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

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

		public static Commit BuildCommit()
		{
			const int StreamRevision = 2;
			const int CommitSequence = 2;
			var streamId = Guid.NewGuid();
			var commitId = Guid.NewGuid();
			var headers = new Dictionary<string, object> { { "Key", "Value" }, { "Key2", (long)1234 }, { "Key3", null } };
			var events = new[]
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
				},
				new EventMessage
				{
					Headers =
					{
						{ "MsgKey1", new Uri("http://www.google.com/") },
						{ "MsgKey4", "some header" }
					},
					Body = new[] { "message body" }
				}
			};

			return new Commit(streamId, StreamRevision, commitId, CommitSequence, headers, events.ToList(), null);
		}
	}
}