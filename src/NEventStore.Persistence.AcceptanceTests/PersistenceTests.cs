
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Diagnostics;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class when_a_commit_header_has_a_name_that_contains_a_period : PersistenceEngineConcern
    {
        private Commit _commit, _persisted;
        private Guid _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid();
            _commit = new Commit(_streamId,
                2,
                Guid.NewGuid(),
                1,
                DateTime.Now,
                new Dictionary<string, object> {{"key.1", "value"}},
                new List<EventMessage> {new EventMessage {Body = new ExtensionMethods.SomeDomainEvent {SomeProperty = "Test"}}});
            IPersistStreams persistence = Persistence;
            persistence.Commit(_commit);
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
        private Commit attempt;
        private DateTime now;
        private Commit persisted;
        private Guid streamId;

        protected override void Context()
        {
            now = SystemTime.UtcNow.AddYears(1);
            streamId = Guid.NewGuid();
            attempt = streamId.BuildAttempt(now);

            Persistence.Commit(attempt);
        }

        protected override void Because()
        {
            persisted = Persistence.GetFrom(streamId, 0, int.MaxValue).First();
        }

        [Fact]
        public void should_correctly_persist_the_stream_identifier()
        {
            persisted.StreamId.ShouldBe(attempt.StreamId);
        }

        [Fact]
        public void should_correctly_persist_the_stream_stream_revision()
        {
            persisted.StreamRevision.ShouldBe(attempt.StreamRevision);
        }

        [Fact]
        public void should_correctly_persist_the_commit_identifier()
        {
            persisted.CommitId.ShouldBe(attempt.CommitId);
        }

        [Fact]
        public void should_correctly_persist_the_commit_sequence()
        {
            persisted.CommitSequence.ShouldBe(attempt.CommitSequence);
        }

        // persistence engines have varying levels of precision with respect to time.
        [Fact]
        public void should_correctly_persist_the_commit_stamp()
        {
            persisted.CommitStamp.Subtract(now).ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void should_correctly_persist_the_headers()
        {
            persisted.Headers.Count.ShouldBe(attempt.Headers.Count);
        }

        [Fact]
        public void should_correctly_persist_the_events()
        {
            persisted.Events.Count.ShouldBe(attempt.Events.Count);
        }

        [Fact]
        public void should_add_the_commit_to_the_set_of_undispatched_commits()
        {
            Persistence.GetUndispatchedCommits().FirstOrDefault(x => x.CommitId == attempt.CommitId).ShouldNotBeNull();
        }

        [Fact]
        public void should_cause_the_stream_to_be_found_in_the_list_of_streams_to_snapshot()
        {
            Persistence.GetStreamsToSnapshot(1).FirstOrDefault(x => x.StreamId == streamId).ShouldNotBeNull();
        }
    }

    public class when_reading_from_a_given_revision : PersistenceEngineConcern

    {
        private const int LoadFromCommitContainingRevision = 3;
        private const int UpToCommitWithContainingRevision = 5;
        private Commit[] _committed;
        private Commit _oldest, _oldest2, _oldest3;
        private Guid _streamId;

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

    public class when_committing_a_stream_with_the_same_revision : PersistenceEngineConcern

    {
        private Commit _attemptWithSameRevision;
        private Exception _thrown;

        protected override void Context()
        {
            Commit attempt = Persistence.CommitSingle();
            _attemptWithSameRevision = attempt.StreamId.BuildAttempt();
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

    public class when_committing_a_stream_with_the_same_commit_sequence : PersistenceEngineConcern

    {
        private Commit _attempt1, _attempt2;
        private Exception _thrown;

        protected override void Context()
        {
            Guid streamId = Guid.NewGuid();
            _attempt1 = streamId.BuildAttempt();
            _attempt2 = streamId.BuildAttempt(eventMessageCount: 3);

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

    public class when_attempting_to_overwrite_a_committed_sequence : PersistenceEngineConcern

    {
        private Commit _failedAttempt;
        private Exception _thrown;

        protected override void Context()
        {
            Guid streamId = Guid.NewGuid();
            Commit successfulAttempt = streamId.BuildAttempt();
            _failedAttempt = streamId.BuildAttempt();

            Persistence.Commit(successfulAttempt);
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
        private Commit _attemptTwice;
        private Exception _thrown;

        protected override void Context()
        {
            _attemptTwice = Persistence.CommitSingle();
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
        private Commit _attempt;

        protected override void Context()
        {
            _attempt = Persistence.CommitSingle();
        }

        protected override void Because()
        {
            Persistence.MarkCommitAsDispatched(_attempt);
        }

        [Fact]
        public void should_no_longer_be_found_in_the_set_of_undispatched_commits()
        {
            Persistence.GetUndispatchedCommits().FirstOrDefault(x => x.CommitId == _attempt.CommitId).ShouldBeNull();
        }
    }

    public class when_committing_more_events_than_the_configured_page_size : PersistenceEngineConcern
    {
        private HashSet<Guid> _committed;
        private ICollection<Guid> _loaded;
        private Guid _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid();
            _committed = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1, _streamId).Select(c => c.CommitId).ToHashSet();
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFrom(_streamId, 0, int.MaxValue).Select(c => c.CommitId).ToLinkedList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
        {
            _loaded.Count.ShouldBe(_committed.Count);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed.All(x => _loaded.Contains(x)).ShouldBeTrue();
            // all commits should be found in loaded collection
        }
    }

    public class when_saving_a_snapshot : PersistenceEngineConcern
    {
        private bool _added;
        private Snapshot _snapshot;
        private Guid _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid();
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
        private Snapshot _correct;
        private Snapshot _snapshot;
        private Guid _streamId;
        private Snapshot _tooFarBack;
        private Snapshot _tooFarForward;

        protected override void Context()
        {
            _streamId = Guid.NewGuid();
            Commit commit1 = Persistence.CommitSingle(_streamId); // rev 1-2
            Commit commit2 = Persistence.CommitNext(commit1); // rev 3-4
            Persistence.CommitNext(commit2); // rev 5-6

            Persistence.AddSnapshot(_tooFarBack = new Snapshot(_streamId, 1, string.Empty));
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
    }

    public class when_a_snapshot_has_been_added_to_the_most_recent_commit_of_a_stream : PersistenceEngineConcern
    {
        private const string SnapshotData = "snapshot";
        private Commit _newest;
        private Commit _oldest, _oldest2;
        private Guid _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid();
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
        private Commit _oldest, _oldest2;
        private Guid _streamId;

        protected override void Context()
        {
            _streamId = Guid.NewGuid();
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
        private Commit[] _committed;
        private Commit _first;
        private DateTime _now;
        private Commit _second;
        private Guid _streamId;
        private Commit _third;

        protected override void Context()
        {
            _streamId = Guid.NewGuid();

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
        private HashSet<Guid> _committed;
        private ICollection<Guid> _loaded;
        private DateTime _start;
        private Guid _streamId;

        protected override void Context()
        {
            _start = SystemTime.UtcNow;
            _committed = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1).Select(c => c.CommitId).ToHashSet();
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFrom(_start).Select(c => c.CommitId).ToLinkedList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
        {
            _loaded.Count.ShouldBeGreaterThanOrEqualTo(_committed.Count);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed.All(x => _loaded.Contains(x)).ShouldBeTrue(); // all commits should be found in loaded collection
        }
    }

    public class when_reading_all_commits_from_the_year_1_AD : PersistenceEngineConcern
    {
        private Exception _thrown;

        protected override void Because()
        {
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

    public class when_persisting_commits_out_of_order : PersistenceEngineConcern
    {
        // Issue 159 OrderingByCommitStampIsNotReliable
        private Commit[] _undispatched;

        protected override void Context()
        {
            Persistence.Purge();
            Guid streamId = Guid.NewGuid();
            var dateTime = new DateTime(2013, 1, 1);
            SystemTime.Resolver = () => dateTime;
            Persistence.Commit(new Commit(streamId,
                1,
                Guid.NewGuid(),
                1,
                SystemTime.UtcNow,
                null,
                new List<EventMessage> {new EventMessage {Body = "M1"}}));
            Persistence.Commit(new Commit(streamId,
                3,
                Guid.NewGuid(),
                3,
                SystemTime.UtcNow,
                null,
                new List<EventMessage> {new EventMessage {Body = "M3"}}));
            Persistence.Commit(new Commit(streamId,
                2,
                Guid.NewGuid(),
                2,
                SystemTime.UtcNow,
                null,
                new List<EventMessage> {new EventMessage {Body = "M2"}}));
        }

        protected override void Because()
        {
            _undispatched = Persistence.GetUndispatchedCommits().ToArray();
        }

        protected override void Cleanup()
        {
            SystemTime.Resolver = null;
        }

        [Fact]
        public void should_have_commits_in_correct_order()
        {
            for (int i = 1; i <= _undispatched.Length; i++)
            {
                _undispatched[i - 1].CommitSequence.ShouldBe(i);
            }
        }
    }

    public class PersistenceEngineConcern : SpecificationBase, IUseFixture<PersistenceEngineFixture>
    {
        private PersistenceEngineFixture _data;

        public IPersistStreams Persistence
        {
            get { return _data.Persistence; }
        }

        public int ConfiguredPageSizeForTesting
        {
            get { return int.Parse("pageSize".GetSetting() ?? "0"); }
        }

        public void SetFixture(PersistenceEngineFixture data)
        {
            _data = data;
        }
    }

    public partial class PersistenceEngineFixture : IDisposable
    {
        private readonly Func<IPersistStreams> _createPersistence;
        private IPersistStreams _persistence;

        public IPersistStreams Persistence
        {
            get
            {
                if (_persistence == null)
                {
                    _persistence = new PerformanceCounterPersistenceEngine(_createPersistence(), "tests");
                    _persistence.Initialize();
                }

                return _persistence;
            }
        }

        public void Dispose()
        {
            if (_persistence != null && !_persistence.IsDisposed)
            {
                _persistence.Purge();
            }

            Persistence.Dispose();
        }
    }
}
