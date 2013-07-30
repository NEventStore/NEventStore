
#pragma warning disable 169

namespace NEventStore.Persistence.RavenPersistence.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class when_committing_a_stream_with_the_same_id_as_a_stream_in_another_partition : using_raven_persistence_with_partitions
    {
        private static IPersistStreams _persistence1, _persistence2;
        private static Commit _attempt1, _attempt2;

        private static Exception _thrown;

        protected override void Context()
        {
            _persistence1 = Partitions.NewEventStoreWithPartition();
            _persistence2 = Partitions.NewEventStoreWithPartition();

            DateTime now = SystemTime.UtcNow;
            _attempt1 = StreamId.BuildAttempt(now);
            _attempt2 = StreamId.BuildAttempt(now.Subtract(TimeSpan.FromDays(1)));

            _persistence1.Commit(_attempt1);
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => _persistence2.Commit(_attempt2));
        }

        [Fact]
        public void should_succeed()
        {
            _thrown.ShouldBeNull();
        }

        [Fact]
        public void should_persist_to_the_correct_partition()
        {
            Commit[] stream = _persistence2.GetFrom(StreamId, 0, int.MaxValue).ToArray();
            stream.ShouldNotBeNull();
            stream.Count().ShouldBe(1);
            stream.First().CommitStamp.ShouldBe(_attempt2.CommitStamp);
        }

        [Fact]
        public void should_not_affect_the_stream_from_the_other_partition()
        {
            Commit[] stream = _persistence1.GetFrom(StreamId, 0, int.MaxValue).ToArray();
            stream.ShouldNotBeNull();
            stream.Count().ShouldBe(1);
            stream.First().CommitStamp.ShouldBe(_attempt1.CommitStamp);
        }
    }

    public class when_saving_a_snapshot_in_a_partition : using_raven_persistence_with_partitions
    {
        private static Snapshot _snapshot;
        private static IPersistStreams _persistence1, _persistence2;
        private static bool _added;

        protected override void Context()
        {
            _snapshot = new Snapshot(StreamId, 1, "Snapshot");
            _persistence1 = Partitions.NewEventStoreWithPartition();
            _persistence2 = Partitions.NewEventStoreWithPartition();
            _persistence1.Commit(StreamId.BuildAttempt());
        }

        protected override void Because()
        {
            _added = _persistence1.AddSnapshot(_snapshot);
        }

        [Fact]
        public void should_indicate_the_snapshot_was_added()
        {
            _added.ShouldBeTrue();
        }

        [Fact]
        public void should_be_able_to_retrieve_the_snapshot()
        {
            _persistence1.GetSnapshot(StreamId, _snapshot.StreamRevision).ShouldNotBeNull();
        }

        [Fact]
        public void should_not_be_able_to_retrieve_the_snapshot_from_another_partition()
        {
            _persistence2.GetSnapshot(StreamId, _snapshot.StreamRevision).ShouldBeNull();
        }
    }

    public class when_reading_all_commits_from_a_particular_point_in_time_from_a_partition : using_raven_persistence_with_partitions
    {
        private static DateTime _now;
        private static IPersistStreams _persistence1, _persistence2;
        private static Commit _first, _second, _third, _fourth, _fifth;
        private static Commit[] _committed1, _committed2;

        protected override void Context()
        {
            _now = SystemTime.UtcNow.AddYears(1);
            _first = Guid.NewGuid().BuildAttempt(_now.AddSeconds(1));
            _second = _first.BuildNextAttempt();
            _third = _second.BuildNextAttempt();
            _fourth = _third.BuildNextAttempt();
            _fifth = Guid.NewGuid().BuildAttempt(_now.AddSeconds(1));

            _persistence1 = Partitions.NewEventStoreWithPartition();
            _persistence2 = Partitions.NewEventStoreWithPartition();

            _persistence1.Commit(_first);
            _persistence1.Commit(_second);
            _persistence1.Commit(_third);
            _persistence1.Commit(_fourth);
            _persistence2.Commit(_fifth);
        }

        protected override void Because()
        {
            _committed1 = _persistence1.GetFrom(_now).ToArray();
        }

        [Fact]
        public void should_return_all_commits_on_or_after_the_point_in_time_specified()
        {
            _committed1.Length.ShouldBe(4);
        }

        [Fact]
        public void should_not_return_commits_from_other_partitions()
        {
            _committed1.Any(c => c.CommitId.Equals(_fifth.CommitId)).ShouldBeFalse();
        }
    }

    public class when_purging_all_commits : using_raven_persistence_with_partitions
    {
        private static IPersistStreams _persistence1, _persistence2;

        protected override void Context()
        {
            _persistence1 = Partitions.NewEventStoreWithPartition();
            _persistence2 = Partitions.NewEventStoreWithPartition();

            _persistence1.Commit(StreamId.BuildAttempt());
            _persistence2.Commit(StreamId.BuildAttempt());
        }

        protected override void Because()
        {
            Thread.Sleep(50); // 50 ms = enough time for Raven to become consistent
            _persistence1.Purge();
        }

        [Fact]
        public void should_purge_all_commits_stored()
        {
            _persistence1.GetFrom(DateTime.MinValue).Count().ShouldBe(0);
        }

        [Fact]
        public void should_purge_all_streams_to_snapshot()
        {
            _persistence1.GetStreamsToSnapshot(0).Count().ShouldBe(0);
        }

        [Fact]
        public void should_purge_all_undispatched_commits()
        {
            _persistence1.GetUndispatchedCommits().Count().ShouldBe(0);
        }

        [Fact]
        public void should_not_purge_all_commits_stored_in_other_partitions()
        {
            _persistence2.GetFrom(DateTime.MinValue).Count().ShouldNotBe(0);
        }

        [Fact]
        public void should_not_purge_all_streams_to_snapshot_in_other_partitions()
        {
            _persistence2.GetStreamsToSnapshot(0).Count().ShouldNotBe(0);
        }

        [Fact]
        public void should_not_purge_all_undispatched_commits_in_other_partitions()
        {
            _persistence2.GetUndispatchedCommits().Count().ShouldNotBe(0);
        }
    }

    public class using_raven_persistence_with_partitions : SpecificationBase, IUseFixture<RavenPartitionedFixture>
    {
        protected Guid StreamId = Guid.NewGuid();

        protected RavenPartitionedFixture Partitions { get; private set; }

        public void SetFixture(RavenPartitionedFixture data)
        {
            Partitions = data;
        }
    }

    public class RavenPartitionedFixture : IDisposable
    {
        private readonly List<IPersistStreams> _instantiatedPersistence = new List<IPersistStreams>();

        public void Dispose()
        {
            foreach (var persistence in _instantiatedPersistence)
            {
                persistence.Dispose();
            }
        }

        public IPersistStreams NewEventStoreWithPartition()
        {
            return NewEventStoreWithPartition(Guid.NewGuid().ToString());
        }

        private IPersistStreams NewEventStoreWithPartition(string partition)
        {
            RavenConfiguration config = TestRavenConfig.GetDefaultConfig();
            config.Partition = partition;

            IPersistStreams persistence = new InMemoryRavenPersistenceFactory(config).Build();
            persistence.Initialize();

            _instantiatedPersistence.Add(persistence);

            return persistence;
        }
    }
}

#pragma warning restore 169