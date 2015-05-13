#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NEventStore.Diagnostics;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

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
            _persisted.Headers.Keys.ShouldContain("key.1");
        }
    }

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
            _persisted.StreamId.ShouldBe(_attempt.StreamId);
        }

        [Fact]
        public void should_correctly_persist_the_stream_stream_revision()
        {
            _persisted.StreamRevision.ShouldBe(_attempt.StreamRevision);
        }

        [Fact]
        public void should_correctly_persist_the_commit_identifier()
        {
            _persisted.CommitId.ShouldBe(_attempt.CommitId);
        }

        [Fact]
        public void should_correctly_persist_the_commit_sequence()
        {
            _persisted.CommitSequence.ShouldBe(_attempt.CommitSequence);
        }

        // persistence engines have varying levels of precision with respect to time.
        [Fact]
        public void should_correctly_persist_the_commit_stamp()
        {
            var difference = _persisted.CommitStamp.Subtract(_now);
            difference.Days.ShouldBe(0);
            difference.Hours.ShouldBe(0);
            difference.Minutes.ShouldBe(0);
            difference.ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void should_correctly_persist_the_headers()
        {
            _persisted.Headers.Count.ShouldBe(_attempt.Headers.Count);
        }

        [Fact]
        public void should_correctly_persist_the_events()
        {
            _persisted.Events.Count.ShouldBe(_attempt.Events.Count);
        }

        [Fact]
        public void should_add_the_commit_to_the_set_of_undispatched_commits()
        {
            Persistence.GetUndispatchedCommits().FirstOrDefault(x => x.CommitId == _attempt.CommitId).ShouldNotBeNull();
        }

        [Fact]
        public void should_cause_the_stream_to_be_found_in_the_list_of_streams_to_snapshot()
        {
            Persistence.GetStreamsToSnapshot(1).FirstOrDefault(x => x.StreamId == _streamId).ShouldNotBeNull();
        }
    }

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
            _committed.First().CommitId.ShouldBe(_oldest2.CommitId); // contains revision 3
        }

        [Fact]
        public void should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified()
        {
            _committed.Last().CommitId.ShouldBe(_oldest3.CommitId); // contains revision 5
        }
    }

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
            _committed.First().CommitId.ShouldBe(_oldest2.CommitId); // contains revision 3
        }

        [Fact]
        public void should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified()
        {
            _committed.Last().CommitId.ShouldBe(_oldest3.CommitId); // contains revision 6
        }
    }

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
            _thrown.ShouldBeInstanceOf<ConcurrencyException>();
        }
    }

    // This test ensure the uniqueness of BucketId+StreamId+CommitSequence 
    // to avoid concurrency issues
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
                _attempt1.StreamRevision +10,
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
            _thrown.ShouldBeInstanceOf<ConcurrencyException>();
        }
    }

    //TODO:This test looks exactly like the one above. What are we trying to prove?
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
            _thrown.ShouldBeInstanceOf<ConcurrencyException>();
        }
    }

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
            _thrown.ShouldBeInstanceOf<DuplicateCommitException>();
        }
    }

    public class when_a_commit_has_been_marked_as_dispatched : PersistenceEngineConcern
    {
        private ICommit _commit;

        protected override void Context()
        {
            _commit = Persistence.CommitSingle();
        }

        protected override void Because()
        {
            Persistence.MarkCommitAsDispatched(_commit);
        }

        [Fact]
        public void should_no_longer_be_found_in_the_set_of_undispatched_commits()
        {
            Persistence.GetUndispatchedCommits().FirstOrDefault(x => x.CommitId == _commit.CommitId).ShouldBeNull();
        }
    }

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
            _loaded.Length.ShouldBe(_committed.Length);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed
                .All(commit => _loaded.SingleOrDefault(loaded => loaded.CommitId == commit.CommitId) != null)
                .ShouldBeTrue();
        }
    }


	public class when_committing_more_streams_than_the_configured_page_size : PersistenceEngineConcern
	{
		private IStreamHead[] _loaded;
		private int _numberOfCommits;

		protected override void Context()
		{
			_numberOfCommits = ConfiguredPageSizeForTesting + 2;
			for (int i = 0; i < _numberOfCommits; i++)
			{
				Persistence.Commit(Guid.NewGuid().ToString().BuildAttempt());
			}
		}

		protected override void Because()
		{
			_loaded = Persistence.GetStreamsToSnapshot(0).ToArray();
		}

		[Fact]
		public void should_load_the_same_number_of_streams_which_have_been_persisted()
		{
			_loaded.Length.ShouldBe(_numberOfCommits);
		}
	}

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
            _added.ShouldBeTrue();
        }

        [Fact]
        public void should_be_able_to_retrieve_the_snapshot()
        {
            Persistence.GetSnapshot(_streamId, _snapshot.StreamRevision).ShouldNotBeNull();
        }
    }

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
            _snapshot.StreamRevision.ShouldBe(_correct.StreamRevision);
        }

        [Fact]
        public void should_have_the_correct_snapshot_payload()
        {
            _snapshot.Payload.ShouldBe(_correct.Payload);
        }

        [Fact]
        public void should_have_the_correct_stream_id()
        {
            _snapshot.StreamId.ShouldBe(_correct.StreamId);
        }
    }

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
            Persistence.GetStreamsToSnapshot(1).Any(x => x.StreamId == _streamId).ShouldBeFalse();
        }
    }

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
            Persistence.GetStreamsToSnapshot(WithinThreshold).FirstOrDefault(x => x.StreamId == _streamId).ShouldNotBeNull();
        }

        [Fact]
        public void should_not_find_the_stream_in_the_set_of_streams_to_be_snapshot_when_over_the_threshold()
        {
            Persistence.GetStreamsToSnapshot(OverThreshold).Any(x => x.StreamId == _streamId).ShouldBeFalse();
        }
    }

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
            _committed.Length.ShouldBe(4);
        }
    }

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
            _loaded.Length.ShouldBe(_committed.Length);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed
                .All(commit => _loaded.SingleOrDefault(loaded => loaded.CommitId == commit.CommitId) != null)
                .ShouldBeTrue();
        }
    }

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
            _loaded = Persistence.GetFrom(checkPoint.ToString()).Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint()
        {
            _loaded.Count.ShouldBe(_committed.Count - checkPoint);
        }

        [Fact]
        public void should_load_only_the_commits_starting_from_the_checkpoint()
        {
            _committed.Skip(checkPoint).All(x => _loaded.Contains(x)).ShouldBeTrue(); // all commits should be found in loaded collection
        }

    }
    public class when_paging_over_all_commits_of_a_bucket_from_a_particular_checkpoint : PersistenceEngineConcern
    {
        private List<Guid> _committedOnBucket1;
        private List<Guid> _committedOnBucket2;
        private ICollection<Guid> _loaded;
        private Guid _streamId;
        private const int checkPoint = 2;

        protected override void Context()
        {
            _committedOnBucket1 = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1,null, "b1").Select(c => c.CommitId).ToList();
            _committedOnBucket2 = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1, null, "b2").Select(c => c.CommitId).ToList();
            _committedOnBucket1.AddRange(Persistence.CommitMany(4, null, "b1").Select(c => c.CommitId));
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFrom("b1", checkPoint.ToString()).Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint()
        {
            _loaded.Count.ShouldBe(_committedOnBucket1.Count - checkPoint);
        }

        [Fact]
        public void should_load_only_the_commits_on_bucket1_starting_from_the_checkpoint()
        {
            _committedOnBucket1.Skip(checkPoint).All(x => _loaded.Contains(x)).ShouldBeTrue(); // all commits should be found in loaded collection
        } 
        
        [Fact]
        public void should_not_load_the_commits_from_bucket2()
        {
            _committedOnBucket2.All(x => !_loaded.Contains(x)).ShouldBeTrue(); 
        }
    }

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
            _thrown.ShouldBeNull();
        }
    }

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
            Persistence.GetFrom(DateTime.MinValue).Count().ShouldBe(0);
        }

        [Fact]
        public void should_not_find_any_streams_to_snapshot()
        {
            Persistence.GetStreamsToSnapshot(0).Count().ShouldBe(0);
        }

        [Fact]
        public void should_not_find_any_undispatched_commits()
        {
            Persistence.GetUndispatchedCommits().Count().ShouldBe(0);
        }
    }

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
            _thrown.ShouldBeInstanceOf<ObjectDisposedException>();
        }
    }

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
            _thrown.ShouldNotBeNull();
        }

        [Fact]
        public void should_be_duplicate_commit_exception()
        {
            _thrown.ShouldBeInstanceOf<ConcurrencyException>();
        }
    }

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
            _thrown.ShouldBeNull();
        }

        [Fact]
        public void should_persist_to_the_correct_bucket()
        {
            ICommit[] stream = Persistence.GetFrom(_bucketBId, _streamId, 0, int.MaxValue).ToArray();
            stream.ShouldNotBeNull();
            stream.Count().ShouldBe(1);
        }

        [Fact]
        public void should_not_affect_the_stream_from_the_other_bucket()
        {
            ICommit[] stream = Persistence.GetFrom(_bucketAId, _streamId, 0, int.MaxValue).ToArray();
            stream.ShouldNotBeNull();
            stream.Count().ShouldBe(1);
            stream.First().CommitStamp.ShouldBe(_attemptACommitStamp);
        }
    }

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
            Persistence.GetSnapshot(_bucketAId, _streamId, _snapshot.StreamRevision).ShouldBeNull();
        }
    }

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
            _returnedCommits.Any(c => c.CommitId.Equals(_commitToBucketB.CommitId)).ShouldBeFalse();
        }
    }

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
            _commits.ShouldNotBeEmpty();
        }

        [Fact]
        public void should_be_in_order_by_checkpoint()
        {
            ICheckpoint checkpoint = Persistence.GetCheckpoint();
            foreach (var commit in _commits)
            {
                ICheckpoint commitCheckpoint = Persistence.GetCheckpoint(commit.CheckpointToken);
                commitCheckpoint.ShouldBeGreaterThan(checkpoint);
                checkpoint = Persistence.GetCheckpoint(commit.CheckpointToken);
            }
        }
    }

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
            Persistence.GetFrom(_bucketAId, DateTime.MinValue).Count().ShouldBe(0);
        }

        [Fact]
        public void should_purge_all_commits_stored_in_bucket_b()
        {
            Persistence.GetFrom(_bucketBId, DateTime.MinValue).Count().ShouldBe(0);
        }

        [Fact]
        public void should_purge_all_streams_to_snapshot_in_bucket_a()
        {
            Persistence.GetStreamsToSnapshot(_bucketAId, 0).Count().ShouldBe(0);
        }

        [Fact]
        public void should_purge_all_streams_to_snapshot_in_bucket_b()
        {
            Persistence.GetStreamsToSnapshot(_bucketBId, 0).Count().ShouldBe(0);
        }

        [Fact]
        public void should_purge_all_undispatched_commits()
        {
            Persistence.GetUndispatchedCommits().Count().ShouldBe(0);
        }
    }

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
                    stream.Add(new EventMessage { Body = i });
                    stream.CommitChanges(Guid.NewGuid());
                }
            }
            ICommit[] commits = Persistence.GetFrom(DateTime.MinValue).ToArray();
            _commits = Persistence.GetFrom().ToArray();
        }

        [Fact]
        public void Should_have_expected_number_of_commits()
        {
            _commits.Length.ShouldBe(_moreThanPageSize);
        }
    }

    /*public class TransactionConcern : PersistenceEngineConcern
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
            _commits.Length.ShouldBe(Loop * StreamsPerTransaction);
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
            commits.Length.ShouldBe(loop);
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
            commits.Length.ShouldBe(0);
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
            commits.Length.ShouldBe(0);
        }
    }*/

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
            commits.Events.Single().Body.ToString().Length.ShouldBe(bodyLength);
        }
    }

    public class PersistenceEngineConcern : SpecificationBase, IUseFixture<PersistenceEngineFixture>
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

        public void SetFixture(PersistenceEngineFixture data)
        {
            _fixture = data;
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
            _persistence = new PerformanceCounterPersistenceEngine(_createPersistence(pageSize), "tests");
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
