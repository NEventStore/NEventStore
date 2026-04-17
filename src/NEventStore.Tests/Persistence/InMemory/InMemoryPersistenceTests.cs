using NEventStore.Persistence.AcceptanceTests.BDD;
#pragma warning disable IDE1006 // Naming Styles

using FluentAssertions;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

namespace NEventStore.Persistence.InMemory
{
#if MSTEST
    [TestClass]
#endif
    public class when_getting_from_to_then_should_not_get_later_commits : SpecificationBase
    {
        private readonly DateTime _endDate = new(2013, 1, 2);
        private readonly DateTime _startDate = new(2013, 1, 1);
        private ICommit[]? _commits;
        private InMemoryPersistenceEngine? _engine;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();
            var streamId = Guid.NewGuid().ToString();
            _engine.Commit(new CommitAttempt(streamId, 1, Guid.NewGuid(), 1, _startDate, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.Commit(new CommitAttempt(streamId, 2, Guid.NewGuid(), 2, _endDate, new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _commits = _engine!.GetFromTo(Bucket.Default, _startDate, _endDate).ToArray();
        }

        [Fact]
        public void should_return_two_commits()
        {
            _commits!.Length.Should().Be(1);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_getting_from_to_checkpoint_then_should_not_get_later_commits : SpecificationBase
    {
        private readonly DateTime _endDate = new(2013, 1, 2);
        private readonly DateTime _startDate = new(2013, 1, 1);
        private ICommit[]? _commits;
        private InMemoryPersistenceEngine? _engine;
        private ICommit? _commit1;
        private ICommit? _commit2;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();
            var streamId = Guid.NewGuid().ToString();
            _commit1 = _engine.Commit(new CommitAttempt(streamId, 1, Guid.NewGuid(), 1, _startDate, new Dictionary<string, object>(), [new EventMessage()]));
            _commit2 = _engine.Commit(new CommitAttempt(streamId, 2, Guid.NewGuid(), 2, _endDate, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.Commit(new CommitAttempt(streamId, 3, Guid.NewGuid(), 3, _endDate.AddDays(1), new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _commits = _engine!.GetFromTo(Bucket.Default, _commit1!.CheckpointToken, _commit2!.CheckpointToken).ToArray();
        }

        [Fact]
        public void should_return_two_commits()
        {
            _commits!.Length.Should().Be(1);
            _commit2!.Should().Be(_commits![0]);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_getting_bucket_commits_after_a_checkpoint_that_only_exists_in_another_bucket : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private ICommit[]? _commits;
        private ICommit? _expected;
        private long _checkpointFromOtherBucket;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // This regression proves bucket checkpoint reads are based on the global checkpoint
            // position, not on whether the lower-bound checkpoint exists inside the same bucket.
            _engine.Commit(new CommitAttempt("b1", "stream-1", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _checkpointFromOtherBucket = _engine.Commit(new CommitAttempt("b2", "stream-2", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]))!.CheckpointToken;
            _expected = _engine.Commit(new CommitAttempt("b1", "stream-1", 2, Guid.NewGuid(), 2, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _commits = _engine!.GetFrom("b1", _checkpointFromOtherBucket).ToArray();
        }

        [Fact]
        public void should_only_return_bucket_commits_after_the_checkpoint()
        {
            _commits!.Should().HaveCount(1);
            _commits[0].Should().Be(_expected);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_getting_stream_commits_from_a_revision_range : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private ICommit[]? _commits;
        private ICommit? _first;
        private ICommit? _second;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // The first commit ends before the requested minimum revision and should be skipped,
            // while the second overlaps the requested range and must still be returned.
            _first = _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 2, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage(), new EventMessage()]));
            _second = _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 4, Guid.NewGuid(), 2, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage(), new EventMessage()]));
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-2", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _commits = _engine!.GetFrom(Bucket.Default, "stream-1", 3, 4).ToArray();
        }

        [Fact]
        public void should_only_return_commits_that_overlap_the_requested_revision_range()
        {
            _commits!.Should().HaveCount(1);
            _commits[0].Should().Be(_second);
            _commits.Should().NotContain(_first);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_adding_a_commit_after_a_snapshot_exists : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private IStreamHead? _streamHead;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // This regression protects the contract that later commits advance HeadRevision without
            // erasing the last known SnapshotRevision for the stream. The new dictionary-based head
            // index must preserve that same pair of values that the old remove/add linked-list
            // implementation produced.
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.AddSnapshot(new Snapshot(Bucket.Default, "stream-1", 1, "snapshot"));
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 2, Guid.NewGuid(), 2, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _streamHead = _engine!.GetStreamsToSnapshot(Bucket.Default, 1).SingleOrDefault(x => x.StreamId == "stream-1");
        }

        [Fact]
        public void should_keep_the_snapshot_revision_on_the_stream_head()
        {
            _streamHead!.SnapshotRevision.Should().Be(1);
        }

        [Fact]
        public void should_advance_the_head_revision_to_the_latest_commit()
        {
            _streamHead!.HeadRevision.Should().Be(2);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_retrieving_the_latest_snapshot_not_beyond_a_revision : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private ISnapshot? _snapshot;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // Multiple snapshots for the same stream must still resolve to the greatest revision
            // that is <= maxRevision. The new per-stream sorted snapshot lists are specifically
            // designed to preserve this selection rule without scanning unrelated streams.
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 6, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage(), new EventMessage(), new EventMessage(), new EventMessage(), new EventMessage(), new EventMessage()]));
            _engine.AddSnapshot(new Snapshot(Bucket.Default, "stream-1", 1, "rev1"));
            _engine.AddSnapshot(new Snapshot(Bucket.Default, "stream-1", 3, "rev3"));
            _engine.AddSnapshot(new Snapshot(Bucket.Default, "stream-1", 5, "rev5"));
            _engine.Commit(new CommitAttempt(Bucket.Default, "other-stream", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.AddSnapshot(new Snapshot(Bucket.Default, "other-stream", 1, "other"));
        }

        protected override void Because()
        {
            _snapshot = _engine!.GetSnapshot(Bucket.Default, "stream-1", 4);
        }

        [Fact]
        public void should_return_the_most_recent_snapshot_not_beyond_the_requested_revision()
        {
            _snapshot!.StreamRevision.Should().Be(3);
        }

        [Fact]
        public void should_return_the_payload_of_that_snapshot()
        {
            _snapshot!.Payload.Should().Be("rev3");
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_adding_a_duplicate_snapshot_revision_for_a_stream : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private bool _added;
        private ISnapshot? _snapshot;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // Duplicate snapshot revisions are intentionally ignored instead of replaced. That
            // behavior existed before the new snapshot index and must remain stable because callers
            // may rely on the first successfully stored snapshot winning for that revision.
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.AddSnapshot(new Snapshot(Bucket.Default, "stream-1", 1, "original"));
        }

        protected override void Because()
        {
            _added = _engine!.AddSnapshot(new Snapshot(Bucket.Default, "stream-1", 1, "duplicate"));
            _snapshot = _engine.GetSnapshot(Bucket.Default, "stream-1", 1);
        }

        [Fact]
        public void should_reject_the_duplicate_snapshot()
        {
            _added.Should().BeFalse();
        }

        [Fact]
        public void should_keep_the_original_snapshot_payload()
        {
            _snapshot!.Payload.Should().Be("original");
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_deleting_a_stream_with_snapshots_and_stream_head : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private ISnapshot? _snapshot;
        private IStreamHead? _streamHead;
        private ICommit[]? _remainingCommits;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // Stream deletion must remove the primary commits and every metadata index that points
            // at them. If the direct dictionaries are not updated in the same critical section, the
            // engine can return stale snapshots or stale stream heads for a deleted stream.
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 2, Guid.NewGuid(), 2, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.AddSnapshot(new Snapshot(Bucket.Default, "stream-1", 1, "snapshot"));
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-2", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _engine!.DeleteStream(Bucket.Default, "stream-1");
            _snapshot = _engine.GetSnapshot(Bucket.Default, "stream-1", int.MaxValue);
            _streamHead = _engine.GetStreamsToSnapshot(Bucket.Default, 0).SingleOrDefault(x => x.StreamId == "stream-1");
            _remainingCommits = _engine.GetFrom(Bucket.Default, "stream-1", 0, int.MaxValue).ToArray();
        }

        [Fact]
        public void should_remove_all_commits_from_the_deleted_stream()
        {
            _remainingCommits!.Should().BeEmpty();
        }

        [Fact]
        public void should_remove_the_snapshot_lookup_entry()
        {
            _snapshot.Should().BeNull();
        }

        [Fact]
        public void should_remove_the_stream_head_lookup_entry()
        {
            _streamHead.Should().BeNull();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging_a_bucket_with_stream_heads_and_snapshots : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private ICommit[]? _bucketACommits;
        private ICommit[]? _bucketBCommits;
        private ISnapshot? _bucketASnapshot;
        private ISnapshot? _bucketBSnapshot;
        private IStreamHead[]? _bucketAHeads;
        private IStreamHead[]? _bucketBHeads;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // Bucket purge should clear every index only for the targeted bucket. This regression
            // protects against sharing state across buckets now that the bucket internals keep more
            // direct lookup structures.
            _engine.Commit(new CommitAttempt("a", "stream-1", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.AddSnapshot(new Snapshot("a", "stream-1", 1, "snapshot-a"));
            _engine.Commit(new CommitAttempt("b", "stream-1", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.AddSnapshot(new Snapshot("b", "stream-1", 1, "snapshot-b"));
        }

        protected override void Because()
        {
            _engine!.Purge("a");
            _bucketACommits = _engine.GetFrom("a", 0).ToArray();
            _bucketBCommits = _engine.GetFrom("b", 0).ToArray();
            _bucketASnapshot = _engine.GetSnapshot("a", "stream-1", int.MaxValue);
            _bucketBSnapshot = _engine.GetSnapshot("b", "stream-1", int.MaxValue);
            _bucketAHeads = _engine.GetStreamsToSnapshot("a", 0).ToArray();
            _bucketBHeads = _engine.GetStreamsToSnapshot("b", 0).ToArray();
        }

        [Fact]
        public void should_clear_commits_snapshots_and_stream_heads_for_the_purged_bucket()
        {
            _bucketACommits!.Should().BeEmpty();
            _bucketASnapshot.Should().BeNull();
            _bucketAHeads!.Should().BeEmpty();
        }

        [Fact]
        public void should_leave_other_buckets_untouched()
        {
            _bucketBCommits!.Should().HaveCount(1);
            _bucketBSnapshot!.Payload.Should().Be("snapshot-b");
            _bucketBHeads!.Should().ContainSingle(x => x.StreamId == "stream-1");
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging_the_store_with_stream_heads_and_snapshots : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private ICommit[]? _commits;
        private ISnapshot? _snapshot;
        private IStreamHead[]? _streamHeads;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // Store-wide purge must be the final safety net that clears every direct lookup index,
            // not just the primary commit collections. Otherwise callers can observe stale metadata
            // after a full purge even though no commits remain anywhere in the store.
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.AddSnapshot(new Snapshot(Bucket.Default, "stream-1", 1, "snapshot"));
        }

        protected override void Because()
        {
            _engine!.Purge();
            _commits = _engine.GetFrom(0).ToArray();
            _snapshot = _engine.GetSnapshot(Bucket.Default, "stream-1", int.MaxValue);
            _streamHeads = _engine.GetStreamsToSnapshot(Bucket.Default, 0).ToArray();
        }

        [Fact]
        public void should_clear_all_metadata_and_commit_indexes()
        {
            _commits!.Should().BeEmpty();
            _snapshot.Should().BeNull();
            _streamHeads!.Should().BeEmpty();
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
