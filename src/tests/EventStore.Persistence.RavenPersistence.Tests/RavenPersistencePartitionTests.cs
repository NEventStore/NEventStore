using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EventStore.Persistence.AcceptanceTests;
using EventStore.Persistence.AcceptanceTests.BDD;
using Xunit;
using Xunit.Should;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Persistence.RavenPersistence.Tests
{
    public class when_committing_a_stream_with_the_same_id_as_a_stream_in_another_partition : using_raven_persistence_with_partitions
    {
        static IPersistStreams persistence1, persistence2;
        static Commit attempt1, attempt2;

        static Exception thrown;

        protected override void Context()
        {
            persistence1 = Partitions.NewEventStoreWithPartition();
            persistence2 = Partitions.NewEventStoreWithPartition();

            var now = SystemTime.UtcNow;
            attempt1 = streamId.BuildAttempt(now);
            attempt2 = streamId.BuildAttempt(now.Subtract(TimeSpan.FromDays(1)));

            persistence1.Commit(attempt1);
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => persistence2.Commit(attempt2));
        }

        [Fact]
        public void should_succeed()
        {
            thrown.ShouldBeNull();
        }

        [Fact]
        public void should_persist_to_the_correct_partition()
        {
            var stream = persistence2.GetFrom(streamId, 0, int.MaxValue).ToArray();
            stream.ShouldNotBeNull();
            stream.Count().ShouldBe(1);
            stream.First().CommitStamp.ShouldBe(attempt2.CommitStamp);
        }

        [Fact]
        public void should_not_affect_the_stream_from_the_other_partition()
        {
            var stream = persistence1.GetFrom(streamId, 0, int.MaxValue).ToArray();
            stream.ShouldNotBeNull();
            stream.Count().ShouldBe(1);
            stream.First().CommitStamp.ShouldBe(attempt1.CommitStamp);
        }
    }

    public class when_saving_a_snapshot_in_a_partition : using_raven_persistence_with_partitions
    {
        static Snapshot snapshot;
        static IPersistStreams persistence1, persistence2;
        static bool added;

        protected override void Context()
        {
            snapshot = new Snapshot(streamId, 1, "Snapshot");
            persistence1 = Partitions.NewEventStoreWithPartition();
            persistence2 = Partitions.NewEventStoreWithPartition();
            persistence1.Commit(streamId.BuildAttempt());
        }

        protected override void Because()
        {
            added = persistence1.AddSnapshot(snapshot);
        }

        [Fact]
        public void should_indicate_the_snapshot_was_added()
        {
            added.ShouldBeTrue();
        }

        [Fact]
        public void should_be_able_to_retrieve_the_snapshot()
        {
            persistence1.GetSnapshot(streamId, snapshot.StreamRevision).ShouldNotBeNull();
        }

        [Fact]
        public void should_not_be_able_to_retrieve_the_snapshot_from_another_partition()
        {
            persistence2.GetSnapshot(streamId, snapshot.StreamRevision).ShouldBeNull();
        }
    }

    public class when_reading_all_commits_from_a_particular_point_in_time_from_a_partition : using_raven_persistence_with_partitions
    {
        static DateTime now;
        static IPersistStreams persistence1, persistence2;
        static Commit first, second, third, fourth, fifth;
        static Commit[] committed1, committed2;

        protected override void Context()
        {
            now = SystemTime.UtcNow.AddYears(1);
            first = Guid.NewGuid().BuildAttempt(now.AddSeconds(1));
            second = first.BuildNextAttempt();
            third = second.BuildNextAttempt();
            fourth = third.BuildNextAttempt();
            fifth = Guid.NewGuid().BuildAttempt(now.AddSeconds(1));

            persistence1 = Partitions.NewEventStoreWithPartition();
            persistence2 = Partitions.NewEventStoreWithPartition();

            persistence1.Commit(first);
            persistence1.Commit(second);
            persistence1.Commit(third);
            persistence1.Commit(fourth);
            persistence2.Commit(fifth);
        }

        protected override void Because()
        {
            committed1 = persistence1.GetFrom(now).ToArray();
        }

        [Fact]
        public void should_return_all_commits_on_or_after_the_point_in_time_specified()
        {
            committed1.Length.ShouldBe(4);
        }

        [Fact]
        public void should_not_return_commits_from_other_partitions()
        {
            committed1.Any(c => c.CommitId.Equals(fifth.CommitId)).ShouldBeFalse();
        }
    }

    public class when_purging_all_commits : using_raven_persistence_with_partitions
    {
        static IPersistStreams persistence1, persistence2;

        protected override void Context()
        {
            persistence1 = Partitions.NewEventStoreWithPartition();
            persistence2 = Partitions.NewEventStoreWithPartition();

            persistence1.Commit(streamId.BuildAttempt());
            persistence2.Commit(streamId.BuildAttempt());
        }

        protected override void Because()
        {
            Thread.Sleep(50); // 50 ms = enough time for Raven to become consistent
            persistence1.Purge();
        }

        [Fact]
        public void should_purge_all_commits_stored()
        {
            persistence1.GetFrom(DateTime.MinValue).Count().ShouldBe(0);
        }

        [Fact]
        public void should_purge_all_streams_to_snapshot()
        {
            persistence1.GetStreamsToSnapshot(0).Count().ShouldBe(0);
        }

        [Fact]
        public void should_purge_all_undispatched_commits()
        {
            persistence1.GetUndispatchedCommits().Count().ShouldBe(0);
        }

        [Fact]
        public void should_not_purge_all_commits_stored_in_other_partitions()
        {
            persistence2.GetFrom(DateTime.MinValue).Count().ShouldNotBe(0);
        }

        [Fact]
        public void should_not_purge_all_streams_to_snapshot_in_other_partitions()
        {
            persistence2.GetStreamsToSnapshot(0).Count().ShouldNotBe(0);
        }

        [Fact]
        public void should_not_purge_all_undispatched_commits_in_other_partitions()
        {
            persistence2.GetUndispatchedCommits().Count().ShouldNotBe(0);
        }
    }

    public class using_raven_persistence_with_partitions : SpecificationBase, IUseFixture<RavenPartitionedFixture>
    {
        protected Guid streamId = Guid.NewGuid();

        public void SetFixture(RavenPartitionedFixture data)
        {
            Partitions = data;
        }

        protected RavenPartitionedFixture Partitions { get; private set; }
    }

    public class RavenPartitionedFixture : IDisposable
    {
        protected List<IPersistStreams> instantiatedPersistence = new List<IPersistStreams>();

        public void Dispose()
        {
            foreach (var persistence in instantiatedPersistence)
            {
                persistence.Dispose();
            }
        }

        public IPersistStreams NewEventStoreWithPartition()
        {
            return NewEventStoreWithPartition(Guid.NewGuid().ToString());
        }

        public IPersistStreams NewEventStoreWithPartition(string partition)
        {
            var config = TestRavenConfig.GetDefaultConfig();
            config.Partition = partition;

            var persistence = new InMemoryRavenPersistenceFactory(config).Build();
            persistence.Initialize();

            instantiatedPersistence.Add(persistence);

            return persistence;
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169