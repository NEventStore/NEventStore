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

        public static ICommit CommitSingle(this IPersistStreams persistence, string streamId = null)
        {
            CommitAttempt commitAttempt = (streamId ?? Guid.NewGuid().ToString()).BuildAttempt();
            return persistence.Commit(commitAttempt);
        }

        public static ICommit CommitNext(this IPersistStreams persistence, ICommit previous)
        {
            var nextAttempt = previous.BuildNextAttempt();
            return persistence.Commit(nextAttempt);
        }

        public static ICommit CommitNext(this IPersistStreams persistence, CommitAttempt previous)
        {
            var nextAttempt = previous.BuildNextAttempt();
            return persistence.Commit(nextAttempt);
        }

        public static IEnumerable<CommitAttempt> CommitMany(this IPersistStreams persistence, int numberOfCommits, string streamId = null)
        {
            var commits = new List<CommitAttempt>();
            CommitAttempt attempt = null;

            for (int i = 0; i < numberOfCommits; i++)
            {
                attempt = attempt == null ? (streamId ?? Guid.NewGuid().ToString()).BuildAttempt() : attempt.BuildNextAttempt();
                persistence.Commit(attempt);
                commits.Add(attempt);
            }

            return commits;
        }

        public static CommitAttempt BuildAttempt(this string streamId, DateTime? now = null, string bucketId = null)
        {
            now = now ?? SystemTime.UtcNow;
            bucketId = bucketId ?? Bucket.Default;

            var messages = new List<EventMessage>
            {
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Test"}},
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Test2"}},
            };

            return new CommitAttempt(bucketId, streamId,
                2,
                Guid.NewGuid(),
                1,
                now.Value,
                new Dictionary<string, object> {{"A header", "A string value"}, {"Another header", 2}},
                messages);
        }

        public static CommitAttempt BuildNextAttempt(this ICommit commit)
        {
            var messages = new List<EventMessage>
            {
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Another test"}},
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Another test2"}},
            };

            return new CommitAttempt(commit.BucketId,
                commit.StreamId,
                commit.StreamRevision + 2,
                Guid.NewGuid(),
                commit.CommitSequence + 1,
                commit.CommitStamp.AddSeconds(1),
                new Dictionary<string, object>(),
                messages);
        }

        public static CommitAttempt BuildNextAttempt(this CommitAttempt commit)
        {
            var messages = new List<EventMessage>
            {
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Another test"}},
                new EventMessage {Body = new SomeDomainEvent {SomeProperty = "Another test2"}},
            };

            return new CommitAttempt(commit.BucketId,
                commit.StreamId,
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

        public static CommitAttempt BuildCommit(this string streamId)
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

            return new CommitAttempt(streamId, streamRevision, commitId, commitSequence, SystemTime.UtcNow, headers, events.ToList());
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