namespace EventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Collections.Generic;

	public static class ExtensionMethods
	{
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> collection)
        {
            return new HashSet<T>(collection);
        }

        public static LinkedList<T> ToLinkedList<T>(this IEnumerable<T> collection)
        {
            return new LinkedList<T>(collection);
        }

        public static Commit CommitSingle(this IPersistStreams persistence, Guid? streamId = null)
        {
            var commit = (streamId ?? Guid.NewGuid()).BuildAttempt();
            persistence.Commit(commit);
            return commit;
        }
        
        public static Commit CommitNext(this IPersistStreams persistence, Commit previous)
        {
            var commit = previous.BuildNextAttempt();
            persistence.Commit(commit);
            return commit;
        }
        
        public static IEnumerable<Commit> CommitMany(this IPersistStreams persistence, int numberOfCommits, Guid? streamId = null)
        {
            var commits = new List<Commit>();
            Commit attempt = null;

            for (int i = 0; i < numberOfCommits; i++)
            {
                attempt = attempt == null ? (streamId ?? Guid.NewGuid()).BuildAttempt() : attempt.BuildNextAttempt();
                persistence.Commit(attempt);
                commits.Add(attempt);
            }

            return commits;
        }
        
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
			return streamId.BuildAttempt(SystemTime.UtcNow);
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
				commit.CommitStamp.AddSeconds(1),
				new Dictionary<string, object>(),
				messages);
		}

		[Serializable]
		public class SomeDomainEvent
		{
			public string SomeProperty { get; set; }

			public override string ToString()
			{
				return this.SomeProperty;
			}
		}
	}
}