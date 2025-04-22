
#pragma warning disable 169 // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

using NEventStore.Persistence.AcceptanceTests.BDD;
using FluentAssertions;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
using NUnit.Framework;
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

namespace NEventStore.Persistence.AcceptanceTests
{
#if MSTEST
    [TestClass]
#endif
    public class when_a_commit_header_has_a_name_that_contains_a_period : PersistenceEngineConcern
    {
        private ICommit? _persisted;
        private string? _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            var attempt = new CommitAttempt(_streamId,
                2,
                Guid.NewGuid(),
                1,
                DateTime.UtcNow,
                new Dictionary<string, object> { { "key.1", "value" } },
                [new EventMessage { Body = new ExtensionMethods.SomeDomainEvent { SomeProperty = "Test" } }]);
            Persistence.Commit(attempt);
        }

        protected override void Because()
        {
            _persisted = Persistence.GetFrom(_streamId!, 0, int.MaxValue).First();
        }

        [Fact]
        public void should_correctly_deserialize_headers()
        {
            _persisted!.Headers.Keys.Should().Contain("key.1");
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_a_commit_is_successfully_persisted : PersistenceEngineConcern
    {
        private CommitAttempt? _attempt;
        private DateTime _now;
        private ICommit? _persisted;
        private string? _streamId;

        protected override void Context()
        {
            _now = SystemTime.UtcNow.AddYears(1);
            _streamId = Guid.NewGuid().ToString();
            _attempt = _streamId.BuildAttempt(_now);

            Persistence.Commit(_attempt);
        }

        protected override void Because()
        {
            _persisted = Persistence.GetFrom(_streamId!, 0, int.MaxValue).First();
        }

        [Fact]
        public void should_correctly_persist_the_stream_identifier()
        {
            _persisted!.StreamId.Should().Be(_attempt!.StreamId);
        }

        [Fact]
        public void should_correctly_persist_the_stream_stream_revision()
        {
            _persisted!.StreamRevision.Should().Be(_attempt!.StreamRevision);
        }

        [Fact]
        public void should_correctly_persist_the_commit_identifier()
        {
            _persisted!.CommitId.Should().Be(_attempt!.CommitId);
        }

        [Fact]
        public void should_correctly_persist_the_commit_sequence()
        {
            _persisted!.CommitSequence.Should().Be(_attempt!.CommitSequence);
        }

        // persistence engines have varying levels of precision with respect to time.
        [Fact]
        public void should_correctly_persist_the_commit_stamp()
        {
            var difference = _persisted!.CommitStamp.Subtract(_now);
            difference.Days.Should().Be(0);
            difference.Hours.Should().Be(0);
            difference.Minutes.Should().Be(0);
            difference.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void should_correctly_persist_the_headers()
        {
            _persisted!.Headers.Count.Should().Be(_attempt!.Headers.Count);
        }

        [Fact]
        public void should_correctly_persist_the_events()
        {
            _persisted!.Events.Count.Should().Be(_attempt!.Events.Count);
        }

        [Fact]
        public void should_cause_the_stream_to_be_found_in_the_list_of_streams_to_snapshot()
        {
            Persistence.GetStreamsToSnapshot(1).FirstOrDefault(x => x.StreamId == _streamId).Should().NotBeNull();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_from_a_given_revision : PersistenceEngineConcern
    {
        private const int LoadFromCommitContainingRevision = 3;
        private const int UpToCommitWithContainingRevision = 5;
        private ICommit[]? _committed;
        private ICommit? _oldest, _oldest2, _oldest3;
        private string? _streamId;

        protected override void Context()
        {
            _oldest = Persistence.CommitSingle(); // 2 events, revision 1-2
            _oldest2 = Persistence.CommitNext(_oldest!); // 2 events, revision 3-4
            _oldest3 = Persistence.CommitNext(_oldest2!); // 2 events, revision 5-6
            Persistence.CommitNext(_oldest3!); // 2 events, revision 7-8

            _streamId = _oldest!.StreamId;
        }

        protected override void Because()
        {
            _committed = Persistence.GetFrom(_streamId!, LoadFromCommitContainingRevision, UpToCommitWithContainingRevision).ToArray();
        }

        [Fact]
        public void should_start_from_the_commit_which_contains_the_min_stream_revision_specified()
        {
            _committed![0].CommitId.Should().Be(_oldest2!.CommitId); // contains revision 3
        }

        [Fact]
        public void should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified()
        {
            _committed!.Last().CommitId.Should().Be(_oldest3!.CommitId); // contains revision 5
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_from_a_given_revision_to_commit_revision : PersistenceEngineConcern
    {
        private const int LoadFromCommitContainingRevision = 3;
        private const int UpToCommitWithContainingRevision = 6;
        private ICommit[]? _committed;
        private ICommit? _oldest, _oldest2, _oldest3;
        private string? _streamId;

        protected override void Context()
        {
            _oldest = Persistence.CommitSingle(); // 2 events, revision 1-2
            _oldest2 = Persistence.CommitNext(_oldest!); // 2 events, revision 3-4
            _oldest3 = Persistence.CommitNext(_oldest2!); // 2 events, revision 5-6
            Persistence.CommitNext(_oldest3!); // 2 events, revision 7-8

            _streamId = _oldest!.StreamId;
        }

        protected override void Because()
        {
            _committed = Persistence.GetFrom(_streamId!, LoadFromCommitContainingRevision, UpToCommitWithContainingRevision).ToArray();
        }

        [Fact]
        public void should_start_from_the_commit_which_contains_the_min_stream_revision_specified()
        {
            _committed![0].CommitId.Should().Be(_oldest2!.CommitId); // contains revision 3
        }

        [Fact]
        public void should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified()
        {
            _committed!.Last().CommitId.Should().Be(_oldest3!.CommitId); // contains revision 6
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_committing_a_stream_with_the_same_revision : PersistenceEngineConcern
    {
        private CommitAttempt? _attemptWithSameRevision;
        private Exception? _thrown;

        protected override void Context()
        {
            var commit = Persistence.CommitSingle();
            _attemptWithSameRevision = commit!.StreamId.BuildAttempt();
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_attemptWithSameRevision!));
        }

        [Fact]
        public void should_throw_a_ConcurrencyException()
        {
            _thrown.Should().BeOfType<ConcurrencyException>();
        }
    }

    // This test ensure the uniqueness of BucketId+StreamId+CommitSequence 
    // to avoid concurrency issues
#if MSTEST
    [TestClass]
#endif
    public class when_committing_a_stream_with_the_same_sequence : PersistenceEngineConcern
    {
        private CommitAttempt? _attempt1, _attempt2;
        private Exception? _thrown;

        protected override void Context()
        {
            string streamId = Guid.NewGuid().ToString();
            _attempt1 = streamId.BuildAttempt();
            _attempt2 = new CommitAttempt(
                _attempt1.BucketId,         // <--- Same bucket
                _attempt1.StreamId,         // <--- Same stream it
                _attempt1.StreamRevision + 10,
                Guid.NewGuid(),
                _attempt1.CommitSequence,   // <--- Same commit seq
                DateTime.UtcNow,
                _attempt1.Headers,
                [
                    new EventMessage(){ Body = new ExtensionMethods.SomeDomainEvent {SomeProperty = "Test 3"}}
                ]
            );

            Persistence.Commit(_attempt1);
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_attempt2!));
        }

        [Fact]
        public void should_throw_a_ConcurrencyException()
        {
            _thrown.Should().BeOfType<ConcurrencyException>();
        }
    }

    //TODO:This test looks exactly like the one above. What are we trying to prove?
#if MSTEST
    [TestClass]
#endif
    public class when_attempting_to_overwrite_a_committed_sequence : PersistenceEngineConcern
    {
        private CommitAttempt? _failedAttempt;
        private Exception? _thrown;

        protected override void Context()
        {
            string streamId = Guid.NewGuid().ToString();
            CommitAttempt successfulAttempt = streamId.BuildAttempt();
            Persistence.Commit(successfulAttempt);
            _failedAttempt = streamId.BuildAttempt();
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_failedAttempt!));
        }

        [Fact]
        public void should_throw_a_ConcurrencyException()
        {
            _thrown.Should().BeOfType<ConcurrencyException>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_attempting_to_persist_a_commit_twice : PersistenceEngineConcern
    {
        private CommitAttempt? _attemptTwice;
        private Exception? _thrown;

        protected override void Context()
        {
            var commit = Persistence.CommitSingle();
            _attemptTwice = new CommitAttempt(
                commit!.BucketId,
                commit.StreamId,
                commit.StreamRevision,
                commit.CommitId,
                commit.CommitSequence,
                commit.CommitStamp,
                commit.Headers.ToDictionary(k => k.Key, v => v.Value),
                commit.Events.ToArray());
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_attemptTwice!));
        }

        [Fact]
        public void should_throw_a_DuplicateCommitException()
        {
            _thrown.Should().BeOfType<DuplicateCommitException>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_attempting_to_persist_a_commitId_twice_on_same_stream : PersistenceEngineConcern
    {
        private CommitAttempt? _attemptTwice;
        private Exception? _thrown;

        protected override void Context()
        {
            var commit = Persistence.CommitSingle();
            _attemptTwice = new CommitAttempt(
                commit!.BucketId,
                commit.StreamId,
                commit.StreamRevision + 1,
                commit.CommitId,
                commit.CommitSequence + 1,
                commit.CommitStamp,
                commit.Headers.ToDictionary(k => k.Key, v => v.Value),
                commit.Events.ToArray()
            );
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_attemptTwice!));
        }

        [Fact]
        public void should_throw_a_DuplicateCommitException()
        {
            _thrown.Should().BeOfType<DuplicateCommitException>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_committing_more_events_than_the_configured_page_size : PersistenceEngineConcern
    {
        private CommitAttempt[]? _committed;
        private ICommit[]? _loaded;
        private string? _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _committed = Persistence.CommitMany(ConfiguredPageSizeForTesting + 2, _streamId).ToArray();
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFrom(_streamId!, 0, int.MaxValue).ToArray();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
        {
            _loaded!.Length.Should().Be(_committed!.Length);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed!
                .All(commit => _loaded!.SingleOrDefault(loaded => loaded.CommitId == commit.CommitId) != null)
                .Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_saving_a_snapshot : PersistenceEngineConcern
    {
        private bool _added;
        private Snapshot? _snapshot;
        private string? _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _snapshot = new Snapshot(_streamId, 1, "Snapshot");
            Persistence.CommitSingle(_streamId);
        }

        protected override void Because()
        {
            _added = Persistence.AddSnapshot(_snapshot!);
        }

        [Fact]
        public void should_indicate_the_snapshot_was_added()
        {
            _added.Should().BeTrue();
        }

        [Fact]
        public void should_be_able_to_retrieve_the_snapshot()
        {
            Persistence.GetSnapshot(_streamId!, _snapshot!.StreamRevision).Should().NotBeNull();
        }
    }

    /// <summary>
    /// having multiple snapshots for the same tuple: bucketId, streamId, streamRevision
    /// should not be allowed, the resulting behavior should be ignoring or updating the
    /// snapshot, that was the original design (it's up to the driver decide what to do)
    /// this behavior can be changed in a future implementation.
    /// </summary>
#if MSTEST
    [TestClass]
#endif
    public class when_adding_multiple_snapshots_for_same_bucketId_streamId_streamRevision : PersistenceEngineConcern
    {
        private bool _added;
        private Snapshot? _snapshot;
        private Snapshot? _updatedSnapshot;
        private string? _streamId;

        private Exception? _thrown;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _snapshot = new Snapshot(_streamId, 1, "Snapshot");
            Persistence.CommitSingle(_streamId);

            Persistence.AddSnapshot(_snapshot);
        }

        protected override void Because()
        {
            _updatedSnapshot = new Snapshot(_streamId!, 1, "Updated Snapshot");
            _thrown = Catch.Exception(() => _added = Persistence.AddSnapshot(_updatedSnapshot));
        }

        [Fact]
        public void should_not_raise_exception()
        {
            _thrown.Should().BeNull();
        }

        [Fact]
        public void should_be_able_to_retrieve_the_correct_snapshot_original_or_updated_depends_on_driver_implementation()
        {
            var snapshot = Persistence.GetSnapshot(_streamId!, _snapshot!.StreamRevision);
            snapshot.Should().NotBeNull();
            if (_added)
            {
                snapshot!.Payload.Should().Be(_updatedSnapshot!.Payload, "The snapshot was added, I expected to get the most updated version");
            }
            else
            {
                snapshot!.Payload.Should().Be(_snapshot.Payload, "The snapshot was not added, I expected to get the original version");
            }
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_retrieving_a_snapshot : PersistenceEngineConcern
    {
        private Snapshot? _correct;
        private ISnapshot? _snapshot;
        private string? _streamId;
        private Snapshot? _tooFarForward;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            var commit1 = Persistence.CommitSingle(_streamId); // rev 1-2
            var commit2 = Persistence.CommitNext(commit1!); // rev 3-4
            Persistence.CommitNext(commit2!); // rev 5-6

            Persistence.AddSnapshot(new Snapshot(_streamId, 1, string.Empty)); //Too far back
            _correct = new Snapshot(_streamId, 3, "Snapshot");
            Persistence.AddSnapshot(_correct);
            _tooFarForward = new Snapshot(_streamId, 5, string.Empty);
            Persistence.AddSnapshot(_tooFarForward);
        }

        protected override void Because()
        {
            _snapshot = Persistence.GetSnapshot(_streamId!, _tooFarForward!.StreamRevision - 1);
        }

        [Fact]
        public void should_load_the_most_recent_prior_snapshot()
        {
            _snapshot!.StreamRevision.Should().Be(_correct!.StreamRevision);
        }

        [Fact]
        public void should_have_the_correct_snapshot_payload()
        {
            _snapshot!.Payload.Should().Be(_correct!.Payload);
        }

        [Fact]
        public void should_have_the_correct_stream_id()
        {
            _snapshot!.StreamId.Should().Be(_correct!.StreamId);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_a_snapshot_has_been_added_to_the_most_recent_commit_of_a_stream : PersistenceEngineConcern
    {
        private const string SnapshotData = "snapshot";
        private ICommit? _newest;
        private ICommit? _oldest, _oldest2;
        private string? _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _oldest = Persistence.CommitSingle(_streamId);
            _oldest2 = Persistence.CommitNext(_oldest!);
            _newest = Persistence.CommitNext(_oldest2!);
        }

        protected override void Because()
        {
            Persistence.AddSnapshot(new Snapshot(_streamId!, _newest!.StreamRevision, SnapshotData));
        }

        [Fact]
        public void should_no_longer_find_the_stream_in_the_set_of_streams_to_be_snapshot()
        {
            Persistence.GetStreamsToSnapshot(1).Any(x => x.StreamId == _streamId).Should().BeFalse();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_adding_a_commit_after_a_snapshot : PersistenceEngineConcern
    {
        private const int WithinThreshold = 2;
        private const int OverThreshold = 3;
        private const string SnapshotData = "snapshot";
        private ICommit? _oldest, _oldest2;
        private string? _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _oldest = Persistence.CommitSingle(_streamId);
            _oldest2 = Persistence.CommitNext(_oldest!);
            Persistence.AddSnapshot(new Snapshot(_streamId, _oldest2!.StreamRevision, SnapshotData));
        }

        protected override void Because()
        {
            Persistence.Commit(_oldest2!.BuildNextAttempt());
        }

        // Because Raven and Mongo update the stream head asynchronously, occasionally will fail this test
        [Fact]
        public void should_find_the_stream_in_the_set_of_streams_to_be_snapshot_when_within_the_threshold()
        {
            Persistence.GetStreamsToSnapshot(WithinThreshold).FirstOrDefault(x => x.StreamId == _streamId).Should().NotBeNull();
        }

        [Fact]
        public void should_not_find_the_stream_in_the_set_of_streams_to_be_snapshot_when_over_the_threshold()
        {
            Persistence.GetStreamsToSnapshot(OverThreshold).Any(x => x.StreamId == _streamId).Should().BeFalse();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_all_commits_from_a_particular_point_in_time : PersistenceEngineConcern
    {
        private ICommit[]? _committed;
        private CommitAttempt? _first;
        private DateTime _now;
        private ICommit? _second;
        private string? _streamId;
        private ICommit? _third;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();

            _now = SystemTime.UtcNow.AddYears(1);
            _first = _streamId.BuildAttempt(_now.AddSeconds(1));
            Persistence.Commit(_first);

            _second = Persistence.CommitNext(_first);
            _third = Persistence.CommitNext(_second!);
            Persistence.CommitNext(_third!);
        }

        protected override void Because()
        {
            _committed = Persistence.GetFrom(Bucket.Default, _now).ToArray();
        }

        [Fact]
        public void should_return_all_commits_on_or_after_the_point_in_time_specified()
        {
            _committed!.Length.Should().Be(4);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_from_a_particular_point_in_time : PersistenceEngineConcern
    {
        private CommitAttempt[]? _committed;
        private ICommit[]? _loaded;
        private DateTime _start;

        protected override void Context()
        {
            _start = SystemTime.UtcNow;
            // Due to loss in precision in various storage engines, we're rounding down to the
            // nearest second to ensure include all commits from the 'start'.
            _start = _start.AddSeconds(-1);
            _committed = Persistence.CommitMany(ConfiguredPageSizeForTesting + 2).ToArray();
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFrom(Bucket.Default, _start).ToArray();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
        {
            _loaded!.Length.Should().Be(_committed!.Length);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed!
                .All(commit => _loaded!.SingleOrDefault(loaded => loaded.CommitId == commit.CommitId) != null)
                .Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_from_a_particular_checkpoint : PersistenceEngineConcern
    {
        private List<Guid>? _committed;
        private List<Guid>? _loaded;
        private const int checkPoint = 2;

        protected override void Context()
        {
            _committed = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1).Select(c => c.CommitId).ToList();
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFrom(checkPoint).Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint()
        {
            _loaded!.Count.Should().Be(_committed!.Count - checkPoint);
        }

        [Fact]
        public void should_load_only_the_commits_starting_from_the_checkpoint()
        {
            _committed!.Skip(checkPoint).All(x => _loaded!.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_of_a_bucket_from_a_particular_checkpoint : PersistenceEngineConcern
    {
        private List<Guid>? _committedOnBucket1;
        private List<Guid>? _committedOnBucket2;
        private List<Guid>? _loaded;
        private const int checkPoint = 2;

        protected override void Context()
        {
            _committedOnBucket1 = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1, null, "b1").Select(c => c.CommitId).ToList();
            _committedOnBucket2 = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1, null, "b2").Select(c => c.CommitId).ToList();
            _committedOnBucket1.AddRange(Persistence.CommitMany(4, null, "b1").Select(c => c.CommitId));
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFrom("b1", checkPoint).Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint()
        {
            _loaded!.Count.Should().Be(_committedOnBucket1!.Count - checkPoint);
        }

        [Fact]
        public void should_load_only_the_commits_on_bucket1_starting_from_the_checkpoint()
        {
            _committedOnBucket1!.Skip(checkPoint).All(x => _loaded!.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }

        [Fact]
        public void should_not_load_the_commits_from_bucket2()
        {
            _committedOnBucket2!.All(x => !_loaded!.Contains(x)).Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_from_a_particular_checkpoint_to_a_checkpoint : PersistenceEngineConcern
    {
        private readonly List<Guid> _committed = [];
        private List<Guid>? _loaded;
        private const int startCheckpoint = 2;
        private int endCheckpoint;

        protected override void Context()
        {
            var committedOnBucket1 = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1, null, Bucket.Default).Select(c => c.CommitId).ToList();
            var committedOnBucket2 = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1, null, "Bucket1").Select(c => c.CommitId).ToList();
            _committed.AddRange(committedOnBucket1);
            _committed.AddRange(committedOnBucket2);
            endCheckpoint = (2 * (ConfiguredPageSizeForTesting + 1)) - 1;
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFromTo(startCheckpoint, endCheckpoint).Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint_to_the_checkpoint()
        {
            _loaded!.Count.Should().Be(endCheckpoint - startCheckpoint);
        }

        [Fact]
        public void should_load_only_the_commits_starting_from_the_checkpoint_to_the_checkpoint()
        {
            _committed
                .Skip(startCheckpoint)
                .Take(_committed.Count - startCheckpoint - 1)
                //.Take(endCheckpoint - startCheckpoint)
                .All(x => _loaded!.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_of_a_bucket_from_a_particular_checkpoint_to_a_checkpoint : PersistenceEngineConcern
    {
        private List<Guid>? _committedOnBucket1;
        private List<Guid>? _committedOnBucket2;
        private List<Guid>? _loaded;
        private const int startCheckpoint = 2;
        private int endCheckpoint;

        protected override void Context()
        {
            _committedOnBucket1 = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1, null, "b1").Select(c => c.CommitId).ToList();
            _committedOnBucket2 = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1, null, "b2").Select(c => c.CommitId).ToList();
            _committedOnBucket1.AddRange(Persistence.CommitMany(4, null, "b1").Select(c => c.CommitId));
            endCheckpoint = ((2 * (ConfiguredPageSizeForTesting + 1)) + 4) - 1;
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFromTo("b1", startCheckpoint, endCheckpoint).Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint_to_the_checkpoint()
        {
            _loaded!.Count.Should().Be(_committedOnBucket1!.Count - startCheckpoint - 1);
        }

        [Fact]
        public void should_load_only_the_commits_on_bucket1_starting_from_the_checkpoint_to_the_checkpoint()
        {
            _committedOnBucket1!
                .Skip(startCheckpoint)
                .Take(_committedOnBucket1!.Count - startCheckpoint - 1)
                .All(x => _loaded!.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }

        [Fact]
        public void should_not_load_the_commits_from_bucket2()
        {
            _committedOnBucket2!.All(x => !_loaded!.Contains(x)).Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_all_commits_from_the_year_1_AD : PersistenceEngineConcern
    {
        private Exception? _thrown;

        protected override void Because()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            _thrown = Catch.Exception(() => Persistence.GetFrom(Bucket.Default, 0).FirstOrDefault());
        }

        [Fact]
        public void should_NOT_throw_an_exception()
        {
            _thrown.Should().BeNull();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging_all_commits : PersistenceEngineConcern
    {
        protected override void Context()
        {
            Persistence.CommitSingle();
        }

        protected override void Because()
        {
            Persistence.Purge();
        }

        [Fact]
        public void should_not_find_any_commits_stored()
        {
            Persistence.GetFrom(Bucket.Default, 0).Count().Should().Be(0);
        }

        [Fact]
        public void should_not_find_any_streams_to_snapshot()
        {
            Persistence.GetStreamsToSnapshot(0).Count().Should().Be(0);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_invoking_after_disposal : PersistenceEngineConcern
    {
        private Exception? _thrown;

        protected override void Context()
        {
            Persistence.Dispose();
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.CommitSingle());
        }

        [Fact]
        public void should_throw_an_ObjectDisposedException()
        {
            _thrown.Should().BeOfType<ObjectDisposedException>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_committing_a_stream_with_the_same_id_as_a_stream_same_bucket : PersistenceEngineConcern
    {
        private string? _streamId;
        private static Exception? _thrown;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            Persistence.Commit(_streamId.BuildAttempt());
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_streamId!.BuildAttempt()));
        }

        [Fact]
        public void should_throw()
        {
            _thrown.Should().NotBeNull();
        }

        [Fact]
        public void should_be_duplicate_commit_exception()
        {
            _thrown.Should().BeOfType<ConcurrencyException>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_committing_a_stream_with_the_same_id_as_a_stream_in_another_bucket : PersistenceEngineConcern
    {
        private const string _bucketAId = "a";
        private const string _bucketBId = "b";
        private string? _streamId;
        private static CommitAttempt? _attemptForBucketB;
        private static Exception? _thrown;
        private DateTime _attemptACommitStamp;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            DateTime now = SystemTime.UtcNow;
            Persistence.Commit(_streamId.BuildAttempt(now, _bucketAId));
            _attemptACommitStamp = Persistence.GetFrom(_bucketAId, _streamId, 0, int.MaxValue).First().CommitStamp;
            _attemptForBucketB = _streamId.BuildAttempt(now.Subtract(TimeSpan.FromDays(1)), _bucketBId);
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_attemptForBucketB!));
        }

        [Fact]
        public void should_succeed()
        {
            _thrown.Should().BeNull();
        }

        [Fact]
        public void should_persist_to_the_correct_bucket()
        {
            ICommit[] stream = Persistence.GetFrom(_bucketBId, _streamId!, 0, int.MaxValue).ToArray();
            stream.Should().NotBeNull();
            stream.Length.Should().Be(1);
        }

        [Fact]
        public void should_not_affect_the_stream_from_the_other_bucket()
        {
            ICommit[] stream = Persistence.GetFrom(_bucketAId, _streamId!, 0, int.MaxValue).ToArray();
            stream.Should().NotBeNull();
            stream.Length.Should().Be(1);
            stream[0].CommitStamp.Should().Be(_attemptACommitStamp);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_saving_a_snapshot_for_a_stream_with_the_same_id_as_a_stream_in_another_bucket : PersistenceEngineConcern
    {
        private const string _bucketAId = "a";
        private const string _bucketBId = "b";

        private string? _streamId;

        private static Snapshot? _snapshot;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _snapshot = new Snapshot(_bucketBId, _streamId, 1, "Snapshot");
            Persistence.Commit(_streamId.BuildAttempt(bucketId: _bucketAId));
            Persistence.Commit(_streamId.BuildAttempt(bucketId: _bucketBId));
        }

        protected override void Because()
        {
            Persistence.AddSnapshot(_snapshot!);
        }

        [Fact]
        public void should_affect_snapshots_from_another_bucket()
        {
            Persistence.GetSnapshot(_bucketAId, _streamId!, _snapshot!.StreamRevision).Should().BeNull();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_all_commits_from_a_particular_point_in_time_and_there_are_streams_in_multiple_buckets : PersistenceEngineConcern
    {
        private const string _bucketAId = "a";
        private const string _bucketBId = "b";

        private static DateTime _now;
        private static ICommit[]? _returnedCommits;
        private CommitAttempt? _commitToBucketB;

        protected override void Context()
        {
            _now = SystemTime.UtcNow.AddYears(1);

            var commitToBucketA = Guid.NewGuid().ToString().BuildAttempt(_now.AddSeconds(1), _bucketAId);

            Persistence.Commit(commitToBucketA);
            commitToBucketA = commitToBucketA.BuildNextAttempt();
            Persistence.Commit(commitToBucketA);
            commitToBucketA = commitToBucketA.BuildNextAttempt();
            Persistence.Commit(commitToBucketA);
            Persistence.Commit(commitToBucketA.BuildNextAttempt());

            _commitToBucketB = Guid.NewGuid().ToString().BuildAttempt(_now.AddSeconds(1), _bucketBId);

            Persistence.Commit(_commitToBucketB);
        }

        protected override void Because()
        {
            _returnedCommits = Persistence.GetFrom(_bucketAId, _now).ToArray();
        }

        [Fact]
        public void should_not_return_commits_from_other_buckets()
        {
            _returnedCommits!.Any(c => c.CommitId.Equals(_commitToBucketB!.CommitId)).Should().BeFalse();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_getting_all_commits_since_checkpoint_and_there_are_streams_in_multiple_buckets : PersistenceEngineConcern
    {
        private ICommit[]? _commits;

        protected override void Context()
        {
            const string bucketAId = "a";
            const string bucketBId = "b";
            Persistence.Commit(Guid.NewGuid().ToString().BuildAttempt(bucketId: bucketAId));
            Persistence.Commit(Guid.NewGuid().ToString().BuildAttempt(bucketId: bucketBId));
            Persistence.Commit(Guid.NewGuid().ToString().BuildAttempt(bucketId: bucketAId));
        }

        protected override void Because()
        {
            _commits = Persistence.GetFrom(0).ToArray();
        }

        [Fact]
        public void should_not_be_empty()
        {
            _commits.Should().NotBeEmpty();
        }

        [Fact]
        public void should_be_in_order_by_checkpoint()
        {
            Int64 checkpoint = 0;
            foreach (var commit in _commits!)
            {
                Int64 commitCheckpoint = commit.CheckpointToken;
                commitCheckpoint.Should().BeGreaterThan(checkpoint);
                checkpoint = commit.CheckpointToken;
            }
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging_all_commits_and_there_are_streams_in_multiple_buckets : PersistenceEngineConcern
    {
        private const string _bucketAId = "a";
        private const string _bucketBId = "b";

        private string? _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            Persistence.Commit(_streamId.BuildAttempt(bucketId: _bucketAId));
            Persistence.Commit(_streamId.BuildAttempt(bucketId: _bucketBId));
            var _snapshotA = new Snapshot(bucketId: _bucketAId, _streamId, 1, "SnapshotA");
            Persistence.AddSnapshot(_snapshotA);
            var _snapshotB = new Snapshot(bucketId: _bucketBId, _streamId, 1, "SnapshotB");
            Persistence.AddSnapshot(_snapshotB);
        }

        protected override void Because()
        {
            Persistence.Purge();
        }

        [Fact]
        public void should_purge_all_commits_stored_in_bucket_a()
        {
            Persistence.GetFrom(_bucketAId, 0).Count().Should().Be(0);
        }

        [Fact]
        public void should_purge_all_commits_stored_in_bucket_b()
        {
            Persistence.GetFrom(_bucketBId, 0).Count().Should().Be(0);
        }

        [Fact]
        public void should_purge_all_streams_to_snapshot_in_bucket_a()
        {
            Persistence.GetStreamsToSnapshot(_bucketAId, 0).Count().Should().Be(0);
        }

        [Fact]
        public void should_purge_all_streams_to_snapshot_in_bucket_b()
        {
            Persistence.GetStreamsToSnapshot(_bucketBId, 0).Count().Should().Be(0);
        }
    }

    [Serializable]
    public class TestEvent
    {
        public String? S { get; set; }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_calling_CommitChanges : PersistenceEngineConcern
    {
        private Guid? _commitId;
        private ICommit? _persistedCommit;
        private ICommit[]? _commits;

        protected override void Because()
        {
            var eventStore = new OptimisticEventStore(Persistence, null, null);
            using IEventStream stream = eventStore.OpenStream(Guid.NewGuid());
            stream.Add(new EventMessage { Body = new TestEvent() { S = "Hi " } });
            _commitId = Guid.NewGuid();
            _persistedCommit = stream.CommitChanges(_commitId.Value);

            _commits = Persistence.GetFrom(0).ToArray();
        }

        [Fact]
        public void A_Commit_had_been_persisted()
        {
            _persistedCommit.Should().NotBeNull();
            _persistedCommit!.CommitId.Should().Be(_commitId!.Value);
            _persistedCommit.CommitSequence.Should().Be(1);
            _persistedCommit.Events.Count.Should().Be(1);
            _persistedCommit.Events.Single().Body.Should().BeOfType<TestEvent>();
            ((TestEvent)_persistedCommit.Events.Single().Body!).S.Should().Be("Hi ");
        }

        [Fact]
        public void Should_have_expected_number_of_commits()
        {
            _commits!.Length.Should().Be(1);
            // if should have the right event
            _commits[0].CommitId.Should().Be(_commitId!.Value);
            _commits[0].Events.Count.Should().Be(1);
            _commits[0].Events.Single().Body.Should().BeOfType<TestEvent>();
            ((TestEvent)_commits[0].Events.Single().Body!).S.Should().Be("Hi ");
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_gettingFromCheckpoint_amount_of_commits_exceeds_PageSize : PersistenceEngineConcern
    {
        private ICommit[]? _commits;
        private int _moreThanPageSize;

        protected override void Because()
        {
            _moreThanPageSize = ConfiguredPageSizeForTesting + 1;
            var eventStore = new OptimisticEventStore(Persistence, null, null);
            // TODO: Not sure how to set the actual page size to the const defined above
            for (int i = 0; i < _moreThanPageSize; i++)
            {
                using IEventStream stream = eventStore.OpenStream(Guid.NewGuid());
                stream.Add(new EventMessage { Body = new TestEvent() { S = "Hi " + i } });
                stream.CommitChanges(Guid.NewGuid());
            }
            _commits = Persistence.GetFrom(0).ToArray();
        }

        [Fact]
        public void Should_have_expected_number_of_commits()
        {
            _commits!.Length.Should().Be(_moreThanPageSize);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_a_payload_is_large : PersistenceEngineConcern
    {
        [Fact]
        public void can_commit()
        {
            const int bodyLength = 100000;
            var attempt = new CommitAttempt(
                Bucket.Default,
                Guid.NewGuid().ToString(),
                1,
                Guid.NewGuid(),
                1,
                DateTime.UtcNow,
                new Dictionary<string, object>(),
                [new EventMessage { Body = new string('a', bodyLength) }]);
            Persistence.Commit(attempt);

            ICommit commits = Persistence.GetFrom(0).Single();
            commits.Events.Single().Body.ToString()!.Length.Should().Be(bodyLength);
        }
    }

    /// <summary>
    /// We are adapting the tests to use 3 different frameworks:
    /// - XUnit: the attached test runner does the job (fixture setup and cleanup)
    /// - NUnit (.net core project)
    /// - MSTest (.net core project)
    /// </summary>
    public abstract class PersistenceEngineConcern : SpecificationBase
#if XUNIT
        , IUseFixture<PersistenceEngineFixture>
#endif
#if NUNIT || MSTEST
        , IDisposable
#endif
    {
        protected PersistenceEngineFixture? Fixture { get; private set; }

        protected IPersistStreams Persistence
        {
            get { return Fixture!.Persistence!; }
        }

        protected int ConfiguredPageSizeForTesting
        {
            get { return 2; }
        }

        /// <summary>
        /// Can be used by XUNIT to initialize the tests.
        /// </summary>
        public void SetFixture(PersistenceEngineFixture data)
        {
            Fixture = data;
            Fixture.Initialize(ConfiguredPageSizeForTesting);
        }

#if NUNIT || MSTEST
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Fixture?.Dispose();
        }

        /// <summary>
        /// <para>
        /// This code was meant to be run right before every test in the fixture to give time
        /// to do further initialization before the PersistenceEngineFixture was created.
        /// Unfortunately the 3 frameworks
        /// have very different ways of doing this:
        /// - NUnit: TestFixtureSetUp
        /// - MSTest: ClassInitialize (not inherited, will be ignored if defined on a base class)
        /// - xUnit: IUseFixture + SetFixture
        /// We need a way to also have some configuration before the PersistenceEngineFixture is created.
        /// </para>
        /// <para>
        /// We've decided to use the test constructor to do the job, it's your responsibility to guarantee
        /// One time initialization (for anything that need it, if you have multiple tests on a fixture)
        /// depending on the framework you are using.
        /// </para>
        /// <para>
        /// quick workaround:
        /// - change the parameters of the "Fixture" properties.
        /// - call the 'Reinitialize()' method can be called to rerun the initialization after we changed the configuration
        /// in the constructor.
        /// or
        /// - call the SetFixture() to reinitialize everything.
        /// </para>
        /// </summary>
        protected PersistenceEngineConcern() : this(new PersistenceEngineFixture())
        { }

        protected PersistenceEngineConcern(PersistenceEngineFixture fixture)
        {
            SetFixture(fixture);
        }
#endif
    }

    public partial class PersistenceEngineFixture : IDisposable
    {
        public IPersistStreams? Persistence { get; private set; }

        private readonly Func<int, IPersistStreams> _createPersistence;

#if NET462
        private bool _tracking = false;
        private string _trackingInstanceName;

        /// <summary>
        /// Automatic Performance Counters and tracking was disabled for full
        /// framework tests because their initialization
        /// can fail when the tests run on build machines (like AppVeyor and similar).
        /// You can enable it back calling this function before <see cref="Initialize(int)"/>
        /// </summary>
        /// <param name="instanceName"></param>
        /// <returns></returns>
        public PersistenceEngineFixture TrackPerformanceInstance(string instanceName = "tests")
        {
            _trackingInstanceName = instanceName;
            _tracking = true;
            return this;
        }
#endif

        public void Initialize(int pageSize)
        {
#if NET462
            // performance counters cab be disabled for full framework tests because their initialization
            // can fail when the tests run on build machines (like AppVeyor and similar)
            if (_tracking)
            {
                Persistence = new NEventStore.Diagnostics.PerformanceCounterPersistenceEngine(_createPersistence(pageSize), _trackingInstanceName);
            }
            else
            {
                Persistence = _createPersistence(pageSize);
            }
#else
            Persistence = _createPersistence(pageSize);
#endif
            Persistence.Initialize();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Persistence?.IsDisposed == false)
            {
                Persistence.Drop();
                Persistence.Dispose();
            }
        }
    }
}

#pragma warning restore 169 // ReSharper disable InconsistentNaming
#pragma warning restore IDE1006 // Naming Styles
