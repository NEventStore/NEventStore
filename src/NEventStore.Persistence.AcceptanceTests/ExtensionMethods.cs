namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        public static Commit CommitSingle(this IPersistStreams persistence, string streamId = null)
        {
            Commit commit = (streamId ?? Guid.NewGuid().ToString()).BuildAttempt();
            persistence.Commit(commit);
            return commit;
        }

        public static Commit CommitNext(this IPersistStreams persistence, Commit previous)
        {
            Commit commit = previous.BuildNextAttempt();
            persistence.Commit(commit);
            return commit;
        }

        public static IEnumerable<Commit> CommitMany(this IPersistStreams persistence, int numberOfCommits, string streamId = null)
        {
            var commits = new List<Commit>();
            Commit attempt = null;

            for (int i = 0; i < numberOfCommits; i++)
            {
                attempt = attempt == null ? (streamId ?? Guid.NewGuid().ToString()).BuildAttempt() : attempt.BuildNextAttempt();
                persistence.Commit(attempt);
                commits.Add(attempt);
            }

            return commits;
        }

        public static Commit BuildAttempt(this string streamId, DateTime now)
        {
            var messages = new List<EventMessage>
            {
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Test"}},
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Test2"}},
            };

            return new Commit(streamId,
                2,
                Guid.NewGuid(),
                1,
                now,
                new Dictionary<string, object> {{"A header", "A string value"}, {"Another header", 2}},
                messages);
        }

        public static Commit BuildAttempt(this string streamId)
        {
            return streamId.BuildAttempt(SystemTime.UtcNow);
        }

        public static Commit BuildNextAttempt(this Commit commit)
        {
            var messages = new List<EventMessage>
            {
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Another test"}},
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Another test2"}},
            };

            return new Commit(commit.StreamId,
                commit.StreamRevision + 2,
                Guid.NewGuid(),
                commit.CommitSequence + 1,
                commit.CommitStamp.AddSeconds(1),
                new Dictionary<string, object>(),
                messages);
        }

        public static SimpleMessage Populate(this SimpleMessage message)
        {
            message = message ?? new SimpleMessage();

            return new SimpleMessage
            {
                Id = Guid.NewGuid(),
                Count = 1234,
                Created = new DateTime(2000, 2, 3, 4, 5, 6, 7).ToUniversalTime(),
                Value = message.Value + "Hello, World!",
                Contents = {"a", null, string.Empty, "d"}
            };
        }

        public static Commit BuildCommit(this string streamId)
        {
            const int streamRevision = 2;
            const int commitSequence = 2;
            Guid commitId = Guid.NewGuid();
            var headers = new Dictionary<string, object> {{"Key", "Value"}, {"Key2", (long) 1234}, {"Key3", null}};
            var events = new[]
            {
                new EventMessage
                {
                    Headers = {{"MsgKey1", TimeSpan.MinValue}, {"MsgKey2", Guid.NewGuid()}, {"MsgKey3", 1.1M}, {"MsgKey4", (ushort) 1}},
                    Body = "some value"
                },
                new EventMessage
                {
                    Headers = {{"MsgKey1", new Uri("http://www.google.com/")}, {"MsgKey4", "some header"}},
                    Body = new[] {"message body"}
                }
            };

            return new Commit(streamId, streamRevision, commitId, commitSequence, SystemTime.UtcNow, headers, events.ToList());
        }

        [Serializable]
        public class SomeDomainEvent
        {
            public string SomeProperty { get; set; }

            public override string ToString()
            {
                return SomeProperty;
            }
        }
    }
}