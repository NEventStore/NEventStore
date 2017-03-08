#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
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
	sing Xunit.Should;
#endif

#if MSTEST
	[TestClass]
#endif
	public class when_a_commit_header_has_a_name_that_contains_a_period : PersistenceEngineConcern
    {
        private ICommit _persisted;
        private string _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            var attempt = new CommitAttempt(_streamId,
                2,
                Guid.NewGuid(),
                1,
                DateTime.Now,
                new Dictionary<string, object> { { "key.1", "value" } },
                new List<EventMessage> { new EventMessage { Body = new ExtensionMethods.SomeDomainEvent { SomeProperty = "Test" } } });
            Persistence.Commit(attempt);
        }

        protected override void Because()
        {
            _persisted = Persistence.GetFrom(_streamId, 0, int.MaxValue).First();
        }

        [Fact]
        public void should_correctly_deserialize_headers()
        {
            _persisted.Headers.Keys.Should().Contain("key.1");
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_a_commit_is_successfully_persisted : PersistenceEngineConcern
    {
        private CommitAttempt _attempt;
        private DateTime _now;
        private ICommit _persisted;
        private string _streamId;

        protected override void Context()
        {
            _now = SystemTime.UtcNow.AddYears(1);
            _streamId = Guid.NewGuid().ToString();
            _attempt = _streamId.BuildAttempt(_now);

            Persistence.Commit(_attempt);
        }

        protected override void Because()
        {
            _persisted = Persistence.GetFrom(_streamId, 0, int.MaxValue).First();
        }

        [Fact]
        public void should_correctly_persist_the_stream_identifier()
        {
            _persisted.StreamId.Should().Be(_attempt.StreamId);
        }

        [Fact]
        public void should_correctly_persist_the_stream_stream_revision()
        {
            _persisted.StreamRevision.Should().Be(_attempt.StreamRevision);
        }

        [Fact]
        public void should_correctly_persist_the_commit_identifier()
        {
            _persisted.CommitId.Should().Be(_attempt.CommitId);
        }

        [Fact]
        public void should_correctly_persist_the_commit_sequence()
        {
            _persisted.CommitSequence.Should().Be(_attempt.CommitSequence);
        }

        // persistence engines have varying levels of precision with respect to time.
        [Fact]
        public void should_correctly_persist_the_commit_stamp()
        {
            var difference = _persisted.CommitStamp.Subtract(_now);
            difference.Days.Should().Be(0);
            difference.Hours.Should().Be(0);
            difference.Minutes.Should().Be(0);
            difference.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void should_correctly_persist_the_headers()
        {
            _persisted.Headers.Count.Should().Be(_attempt.Headers.Count);
        }

        [Fact]
        public void should_correctly_persist_the_events()
        {
            _persisted.Events.Count.Should().Be(_attempt.Events.Count);
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
        private ICommit[] _committed;
        private ICommit _oldest, _oldest2, _oldest3;
        private string _streamId;

        protected override void Context()
        {
            _oldest = Persistence.CommitSingle(); // 2 events, revision 1-2
            _oldest2 = Persistence.CommitNext(_oldest); // 2 events, revision 3-4
            _oldest3 = Persistence.CommitNext(_oldest2); // 2 events, revision 5-6
            Persistence.CommitNext(_oldest3); // 2 events, revision 7-8

            _streamId = _oldest.StreamId;
        }

        protected override void Because()
        {
            _committed = Persistence.GetFrom(_streamId, LoadFromCommitContainingRevision, UpToCommitWithContainingRevision).ToArray();
        }

        [Fact]
        public void should_start_from_the_commit_which_contains_the_min_stream_revision_specified()
        {
            _committed.First().CommitId.Should().Be(_oldest2.CommitId); // contains revision 3
        }

        [Fact]
        public void should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified()
        {
            _committed.Last().CommitId.Should().Be(_oldest3.CommitId); // contains revision 5
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_reading_from_a_given_revision_to_commit_revision : PersistenceEngineConcern
    {
        private const int LoadFromCommitContainingRevision = 3;
        private const int UpToCommitWithContainingRevision = 6;
        private ICommit[] _committed;
        private ICommit _oldest, _oldest2, _oldest3;
        private string _streamId;

        protected override void Context()
        {
            _oldest = Persistence.CommitSingle(); // 2 events, revision 1-2
            _oldest2 = Persistence.CommitNext(_oldest); // 2 events, revision 3-4
            _oldest3 = Persistence.CommitNext(_oldest2); // 2 events, revision 5-6
            Persistence.CommitNext(_oldest3); // 2 events, revision 7-8

            _streamId = _oldest.StreamId;
        }

        protected override void Because()
        {
            _committed = Persistence.GetFrom(_streamId, LoadFromCommitContainingRevision, UpToCommitWithContainingRevision).ToArray();
        }

        [Fact]
        public void should_start_from_the_commit_which_contains_the_min_stream_revision_specified()
        {
            _committed.First().CommitId.Should().Be(_oldest2.CommitId); // contains revision 3
        }

        [Fact]
        public void should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified()
        {
            _committed.Last().CommitId.Should().Be(_oldest3.CommitId); // contains revision 6
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_committing_a_stream_with_the_same_revision : PersistenceEngineConcern
    {
        private CommitAttempt _attemptWithSameRevision;
        private Exception _thrown;

        protected override void Context()
        {
            ICommit commit = Persistence.CommitSingle();
            _attemptWithSameRevision = commit.StreamId.BuildAttempt();
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_attemptWithSameRevision));
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
        private CommitAttempt _attempt1, _attempt2;
        private Exception _thrown;

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
                new[]
                {
                    new EventMessage(){ Body = new ExtensionMethods.SomeDomainEvent {SomeProperty = "Test 3"}}
                }
            );

            Persistence.Commit(_attempt1);
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_attempt2));
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
        private CommitAttempt _failedAttempt;
        private Exception _thrown;

        protected override void Context()
        {
            string streamId = Guid.NewGuid().ToString();
            CommitAttempt successfulAttempt = streamId.BuildAttempt();
            Persistence.Commit(successfulAttempt);
            _failedAttempt = streamId.BuildAttempt();
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_failedAttempt));
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
        private CommitAttempt _attemptTwice;
        private Exception _thrown;

        protected override void Context()
        {
            var commit = Persistence.CommitSingle();
            _attemptTwice = new CommitAttempt(
                commit.BucketId,
                commit.StreamId,
                commit.StreamRevision,
                commit.CommitId,
                commit.CommitSequence,
                commit.CommitStamp,
                commit.Headers,
                commit.Events);
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_attemptTwice));
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
        private CommitAttempt _attemptTwice;
        private Exception _thrown;

        protected override void Context()
        {
            var commit = Persistence.CommitSingle();
            _attemptTwice = new CommitAttempt(
                commit.BucketId,
                commit.StreamId,
                commit.StreamRevision + 1,
                commit.CommitId,
                commit.CommitSequence + 1,
                commit.CommitStamp,
                commit.Headers,
                commit.Events
            );
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_attemptTwice));
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
        private CommitAttempt[] _committed;
        private ICommit[] _loaded;
        private string _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _committed = Persistence.CommitMany(ConfiguredPageSizeForTesting + 2, _streamId).ToArray();
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFrom(_streamId, 0, int.MaxValue).ToArray();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
        {
            _loaded.Length.Should().Be(_committed.Length);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed
                .All(commit => _loaded.SingleOrDefault(loaded => loaded.CommitId == commit.CommitId) != null)
                .Should().BeTrue();
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_saving_a_snapshot : PersistenceEngineConcern
    {
        private bool _added;
        private Snapshot _snapshot;
        private string _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _snapshot = new Snapshot(_streamId, 1, "Snapshot");
            Persistence.CommitSingle(_streamId);
        }

        protected override void Because()
        {
            _added = Persistence.AddSnapshot(_snapshot);
        }

        [Fact]
        public void should_indicate_the_snapshot_was_added()
        {
            _added.Should().BeTrue();
        }

        [Fact]
        public void should_be_able_to_retrieve_the_snapshot()
        {
            Persistence.GetSnapshot(_streamId, _snapshot.StreamRevision).Should().NotBeNull();
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_retrieving_a_snapshot : PersistenceEngineConcern
    {
        private ISnapshot _correct;
        private ISnapshot _snapshot;
        private string _streamId;
        private ISnapshot _tooFarForward;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            ICommit commit1 = Persistence.CommitSingle(_streamId); // rev 1-2
            ICommit commit2 = Persistence.CommitNext(commit1); // rev 3-4
            Persistence.CommitNext(commit2); // rev 5-6

            Persistence.AddSnapshot(new Snapshot(_streamId, 1, string.Empty)); //Too far back
            Persistence.AddSnapshot(_correct = new Snapshot(_streamId, 3, "Snapshot"));
            Persistence.AddSnapshot(_tooFarForward = new Snapshot(_streamId, 5, string.Empty));
        }

        protected override void Because()
        {
            _snapshot = Persistence.GetSnapshot(_streamId, _tooFarForward.StreamRevision - 1);
        }

        [Fact]
        public void should_load_the_most_recent_prior_snapshot()
        {
            _snapshot.StreamRevision.Should().Be(_correct.StreamRevision);
        }

        [Fact]
        public void should_have_the_correct_snapshot_payload()
        {
            _snapshot.Payload.Should().Be(_correct.Payload);
        }

        [Fact]
        public void should_have_the_correct_stream_id()
        {
            _snapshot.StreamId.Should().Be(_correct.StreamId);
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_a_snapshot_has_been_added_to_the_most_recent_commit_of_a_stream : PersistenceEngineConcern
    {
        private const string SnapshotData = "snapshot";
        private ICommit _newest;
        private ICommit _oldest, _oldest2;
        private string _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _oldest = Persistence.CommitSingle(_streamId);
            _oldest2 = Persistence.CommitNext(_oldest);
            _newest = Persistence.CommitNext(_oldest2);
        }

        protected override void Because()
        {
            Persistence.AddSnapshot(new Snapshot(_streamId, _newest.StreamRevision, SnapshotData));
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
        private ICommit _oldest, _oldest2;
        private string _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _oldest = Persistence.CommitSingle(_streamId);
            _oldest2 = Persistence.CommitNext(_oldest);
            Persistence.AddSnapshot(new Snapshot(_streamId, _oldest2.StreamRevision, SnapshotData));
        }

        protected override void Because()
        {
            Persistence.Commit(_oldest2.BuildNextAttempt());
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
        private ICommit[] _committed;
        private CommitAttempt _first;
        private DateTime _now;
        private ICommit _second;
        private string _streamId;
        private ICommit _third;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();

            _now = SystemTime.UtcNow.AddYears(1);
            _first = _streamId.BuildAttempt(_now.AddSeconds(1));
            Persistence.Commit(_first);

            _second = Persistence.CommitNext(_first);
            _third = Persistence.CommitNext(_second);
            Persistence.CommitNext(_third);
        }

        protected override void Because()
        {
            _committed = Persistence.GetFrom(_now).ToArray();
        }

        [Fact]
        public void should_return_all_commits_on_or_after_the_point_in_time_specified()
        {
            _committed.Length.Should().Be(4);
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_paging_over_all_commits_from_a_particular_point_in_time : PersistenceEngineConcern
    {
        private CommitAttempt[] _committed;
        private ICommit[] _loaded;
        private DateTime _start;
        private Guid _streamId;

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
            _loaded = Persistence.GetFrom(_start).ToArray();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
        {
            _loaded.Length.Should().Be(_committed.Length);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed
                .All(commit => _loaded.SingleOrDefault(loaded => loaded.CommitId == commit.CommitId) != null)
                .Should().BeTrue();
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_paging_over_all_commits_from_a_particular_checkpoint : PersistenceEngineConcern
    {
        private List<Guid> _committed;
        private ICollection<Guid> _loaded;
        private Guid _streamId;
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
            _loaded.Count.Should().Be(_committed.Count - checkPoint);
        }

        [Fact]
        public void should_load_only_the_commits_starting_from_the_checkpoint()
        {
            _committed.Skip(checkPoint).All(x => _loaded.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_paging_over_all_commits_of_a_bucket_from_a_particular_checkpoint : PersistenceEngineConcern
    {
        private List<Guid> _committedOnBucket1;
        private List<Guid> _committedOnBucket2;
        private ICollection<Guid> _loaded;
        private Guid _streamId;
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
            _loaded.Count.Should().Be(_committedOnBucket1.Count - checkPoint);
        }

        [Fact]
        public void should_load_only_the_commits_on_bucket1_starting_from_the_checkpoint()
        {
            _committedOnBucket1.Skip(checkPoint).All(x => _loaded.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }

        [Fact]
        public void should_not_load_the_commits_from_bucket2()
        {
            _committedOnBucket2.All(x => !_loaded.Contains(x)).Should().BeTrue();
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_reading_all_commits_from_the_year_1_AD : PersistenceEngineConcern
    {
        private Exception _thrown;

        protected override void Because()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            _thrown = Catch.Exception(() => Persistence.GetFrom(DateTime.MinValue).FirstOrDefault());
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
            Persistence.GetFrom(DateTime.MinValue).Count().Should().Be(0);
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
        private Exception _thrown;

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
        private string _streamId;
        private static Exception _thrown;
        private DateTime _attemptACommitStamp;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            Persistence.Commit(_streamId.BuildAttempt());
        }

        protected override void Because()
        {
            _thrown = Catch.Exception(() => Persistence.Commit(_streamId.BuildAttempt()));
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
        const string _bucketAId = "a";
        const string _bucketBId = "b";
        private string _streamId;
        private static CommitAttempt _attemptForBucketB;
        private static Exception _thrown;
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
            _thrown = Catch.Exception(() => Persistence.Commit(_attemptForBucketB));
        }

        [Fact]
        public void should_succeed()
        {
            _thrown.Should().BeNull();
        }

        [Fact]
        public void should_persist_to_the_correct_bucket()
        {
            ICommit[] stream = Persistence.GetFrom(_bucketBId, _streamId, 0, int.MaxValue).ToArray();
            stream.Should().NotBeNull();
            stream.Count().Should().Be(1);
        }

        [Fact]
        public void should_not_affect_the_stream_from_the_other_bucket()
        {
            ICommit[] stream = Persistence.GetFrom(_bucketAId, _streamId, 0, int.MaxValue).ToArray();
            stream.Should().NotBeNull();
            stream.Count().Should().Be(1);
            stream.First().CommitStamp.Should().Be(_attemptACommitStamp);
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_saving_a_snapshot_for_a_stream_with_the_same_id_as_a_stream_in_another_bucket : PersistenceEngineConcern
    {
        const string _bucketAId = "a";
        const string _bucketBId = "b";

        string _streamId;

        private static Snapshot _snapshot;

        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            _snapshot = new Snapshot(_bucketBId, _streamId, 1, "Snapshot");
            Persistence.Commit(_streamId.BuildAttempt(bucketId: _bucketAId));
            Persistence.Commit(_streamId.BuildAttempt(bucketId: _bucketBId));
        }

        protected override void Because()
        {
            Persistence.AddSnapshot(_snapshot);
        }

        [Fact]
        public void should_affect_snapshots_from_another_bucket()
        {
            Persistence.GetSnapshot(_bucketAId, _streamId, _snapshot.StreamRevision).Should().BeNull();
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_reading_all_commits_from_a_particular_point_in_time_and_there_are_streams_in_multiple_buckets : PersistenceEngineConcern
    {
        const string _bucketAId = "a";
        const string _bucketBId = "b";

        private static DateTime _now;
        private static ICommit[] _returnedCommits;
        private CommitAttempt _commitToBucketB;

        protected override void Context()
        {
            _now = SystemTime.UtcNow.AddYears(1);

            var commitToBucketA = Guid.NewGuid().ToString().BuildAttempt(_now.AddSeconds(1), _bucketAId);

            Persistence.Commit(commitToBucketA);
            Persistence.Commit(commitToBucketA = commitToBucketA.BuildNextAttempt());
            Persistence.Commit(commitToBucketA = commitToBucketA.BuildNextAttempt());
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
            _returnedCommits.Any(c => c.CommitId.Equals(_commitToBucketB.CommitId)).Should().BeFalse();
        }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_getting_all_commits_since_checkpoint_and_there_are_streams_in_multiple_buckets : PersistenceEngineConcern
    {
        private ICommit[] _commits;

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
            _commits = Persistence.GetFromStart().ToArray();
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
            foreach (var commit in _commits)
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
        const string _bucketAId = "a";
        const string _bucketBId = "b";

        string _streamId;
        protected override void Context()
        {
            _streamId = Guid.NewGuid().ToString();
            Persistence.Commit(_streamId.BuildAttempt(bucketId: _bucketAId));
            Persistence.Commit(_streamId.BuildAttempt(bucketId: _bucketBId));
        }

        protected override void Because()
        {
            Persistence.Purge();
        }

        [Fact]
        public void should_purge_all_commits_stored_in_bucket_a()
        {
            Persistence.GetFrom(_bucketAId, DateTime.MinValue).Count().Should().Be(0);
        }

        [Fact]
        public void should_purge_all_commits_stored_in_bucket_b()
        {
            Persistence.GetFrom(_bucketBId, DateTime.MinValue).Count().Should().Be(0);
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
    public class Pippo
    {
        public String S { get; set; }
    }

#if MSTEST
	[TestClass]
#endif
	public class when_gettingfromcheckpoint_amount_of_commits_exceeds_pagesize : PersistenceEngineConcern
    {
        private ICommit[] _commits;
        private int _moreThanPageSize;

        protected override void Because()
        {
            _moreThanPageSize = ConfiguredPageSizeForTesting + 1;
            var eventStore = new OptimisticEventStore(Persistence, null);
            // TODO: Not sure how to set the actual pagesize to the const defined above
            for (int i = 0; i < _moreThanPageSize; i++)
            {
                using (IEventStream stream = eventStore.OpenStream(Guid.NewGuid()))
                {
                    stream.Add(new EventMessage { Body = new Pippo() {S = "Hi " + i} });
                    stream.CommitChanges(Guid.NewGuid());
                }
            }
            ICommit[] commits = Persistence.GetFrom(DateTime.MinValue).ToArray();
            _commits = Persistence.GetFrom().ToArray();
        }

        [Fact]
        public void Should_have_expected_number_of_commits()
        {
            _commits.Length.Should().Be(_moreThanPageSize);
        }
    }

	/*
#if MSTEST
	[TestClass]
#endif
	public class TransactionConcern : PersistenceEngineConcern
    {
        private ICommit[] _commits;
        private const int Loop = 2;
        private const int StreamsPerTransaction = 20;

        protected override void Because()
        {
            Parallel.For(0, Loop, i =>
            {
                var eventStore = new OptimisticEventStore(Persistence, null);
                using (var scope = new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions {IsolationLevel = IsolationLevel.Serializable}))
                {
                    int j;
                    for (j = 0; j < StreamsPerTransaction; j++)
                    {
                        using (var stream = eventStore.OpenStream(i.ToString() + "-" + j.ToString()))
                        {
                            for (int k = 0; k < 10; k++)
                            {
                                stream.Add(new EventMessage {Body = "body" + k});
                            }
                            stream.CommitChanges(Guid.NewGuid());
                        }
                    }
                    scope.Complete();
                }
            });
            _commits = Persistence.GetFrom().ToArray();
        }

        [Fact]
        public void Should_have_expected_number_of_commits()
        {
            _commits.Length.Should().Be(Loop * StreamsPerTransaction);
        }

        [Fact]
        public void ScopeCompleteAndSerializable()
        {
            Reinitialize();
            const int loop = 10;
            using (var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.Serializable
                }))
            {
                Parallel.For(0, loop, i =>
                {
                    Console.WriteLine("Creating stream {0} on thread {1}", i, Thread.CurrentThread.ManagedThreadId);
                    var eventStore = new OptimisticEventStore(Persistence, null);
                    string streamId = i.ToString(CultureInfo.InvariantCulture);
                    using (var stream = eventStore.OpenStream(streamId))
                    {
                        stream.Add(new EventMessage { Body = "body1" });
                        stream.Add(new EventMessage { Body = "body2" });
                        stream.CommitChanges(Guid.NewGuid());
                    }
                });
                scope.Complete();
            }
            ICheckpoint checkpoint = Persistence.GetCheckpoint();
            ICommit[] commits = Persistence.GetFrom(checkpoint.Value).ToArray();
            commits.Length.Should().Be(loop);
        }

        [Fact]
        public void ScopeNotCompleteAndReadCommitted()
        {
            Reinitialize();
            const int loop = 10;
            using(new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }))
            {
                Parallel.For(0, loop, i =>
                {
                    Console.WriteLine(@"Creating stream {0} on thread {1}", i, Thread.CurrentThread.ManagedThreadId);
                    var eventStore = new OptimisticEventStore(Persistence, null);
                    string streamId = i.ToString(CultureInfo.InvariantCulture);
                    using (var stream = eventStore.OpenStream(streamId))
                    {
                        stream.Add(new EventMessage { Body = "body1" });
                        stream.Add(new EventMessage { Body = "body2" });
                        stream.CommitChanges(Guid.NewGuid());
                    }
                });
            }
            ICheckpoint checkpoint = Persistence.GetCheckpoint();
            ICommit[] commits = Persistence.GetFrom(checkpoint.Value).ToArray();
            commits.Length.Should().Be(0);
        }

        [Fact]
        public void ScopeNotCompleteAndSerializable()
        {
            Reinitialize();
            const int loop = 10;
            using(new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }))
            {
                Parallel.For(0, loop, i =>
                {
                    Console.WriteLine(@"Creating stream {0} on thread {1}", i, Thread.CurrentThread.ManagedThreadId);
                    var eventStore = new OptimisticEventStore(Persistence, null);
                    string streamId = i.ToString(CultureInfo.InvariantCulture);
                    using (var stream = eventStore.OpenStream(streamId))
                    {
                        stream.Add(new EventMessage { Body = "body1" });
                        stream.Add(new EventMessage { Body = "body2" });
                        stream.CommitChanges(Guid.NewGuid());
                    }
                });
            }
            ICheckpoint checkpoint = Persistence.GetCheckpoint();
            ICommit[] commits = Persistence.GetFrom(checkpoint.Value).ToArray();
            commits.Length.Should().Be(0);
        }
    }
	*/

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
                new List<EventMessage> { new EventMessage { Body = new string('a', bodyLength) } });
            Persistence.Commit(attempt);

            ICommit commits = Persistence.GetFrom().Single();
            commits.Events.Single().Body.ToString().Length.Should().Be(bodyLength);
        }
    }

    public abstract class PersistenceEngineConcern : SpecificationBase, IDisposable
    {
        private PersistenceEngineFixture _fixture;

        protected IPersistStreams Persistence
        {
            get { return _fixture.Persistence; ; }
        }

        protected int ConfiguredPageSizeForTesting
        {
            get { return 2; }
        }

        protected void Reinitialize()
        {
            _fixture.Initialize(ConfiguredPageSizeForTesting);
        }

		public void Dispose()
		{
			if (_fixture != null)
			{
				_fixture.Dispose();
			}
		}

		protected PersistenceEngineConcern()
		{
			_fixture = new PersistenceEngineFixture();
			_fixture.Initialize(ConfiguredPageSizeForTesting);
		}
    }

    public partial class PersistenceEngineFixture : IDisposable
    {
        private readonly Func<int, IPersistStreams> _createPersistence;
        private IPersistStreams _persistence;

        public void Initialize(int pageSize)
        {
            if (_persistence != null && !_persistence.IsDisposed)
            {
                _persistence.Drop();
                _persistence.Dispose();
            }
#if !NETSTANDARD1_6
			_persistence = new PerformanceCounterPersistenceEngine(_createPersistence(pageSize), "tests");
#else
			_persistence = _createPersistence(pageSize);
#endif
			_persistence.Initialize();
        }

        public IPersistStreams Persistence
        {
            get { return _persistence; }
        }

        public void Dispose()
        {
            if (_persistence != null && !_persistence.IsDisposed)
            {
                _persistence.Drop();
                _persistence.Dispose();
            }
        }
    }
    // ReSharper restore InconsistentNaming
}
