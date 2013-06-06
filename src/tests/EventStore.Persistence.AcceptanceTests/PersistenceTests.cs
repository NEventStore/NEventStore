using EventStore.Diagnostics;
using EventStore.Persistence.AcceptanceTests.BDD;
using Xunit;
using Xunit.Should;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Persistence.AcceptanceTests
{
	public class when_a_commit_header_has_a_name_that_contains_a_period : PersistenceEngineConcern 
    {
        Guid streamId;
        Commit commit, persisted;

        protected override void Context()
        {
            streamId = Guid.NewGuid();
            commit = new Commit(streamId, 2, Guid.NewGuid(), 1, DateTime.Now, 
                new Dictionary<string, object> {{"key.1", "value"}},
                new List<EventMessage>
                {
                    new EventMessage {Body = new ExtensionMethods.SomeDomainEvent {SomeProperty = "Test"}}
                });
            var persistence = Persistence;
            persistence.Commit(commit);
        }

        protected override void Because()
        {
            persisted = Persistence.GetFrom(streamId, 0, int.MaxValue).First();
        }

        [Fact]
        public void should_correctly_deserialize_headers()
        {
            persisted.Headers.Keys.ShouldContain("key.1");
        }
    }

    public class when_a_commit_is_successfully_persisted : PersistenceEngineConcern
        
	{
        Guid streamId;
	    DateTime now;
		Commit attempt, persisted;

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

	    public void should_correctly_persist_the_stream_identifier()
	    {
	        persisted.StreamId.ShouldBe(attempt.StreamId);
	    }

	    public void should_correctly_persist_the_stream_stream_revision()
	    {
	        persisted.StreamRevision.ShouldBe(attempt.StreamRevision);
	    }

	    public void should_correctly_persist_the_commit_identifier()
	    {
	        persisted.CommitId.ShouldBe(attempt.CommitId);
	    }

	    public void should_correctly_persist_the_commit_sequence()
	    {
	        persisted.CommitSequence.ShouldBe(attempt.CommitSequence);
	    }

	    // persistence engines have varying levels of precision with respect to time.
	    public void should_correctly_persist_the_commit_stamp()
	    {
	        persisted.CommitStamp.Subtract(now).ShouldBeLessThan(TimeSpan.FromSeconds(1));
	    }

	    public void should_correctly_persist_the_headers()
	    {
	        persisted.Headers.Count.ShouldBe(attempt.Headers.Count);
	    }

	    public void should_correctly_persist_the_events()
	    {
	        persisted.Events.Count.ShouldBe(attempt.Events.Count);
	    }

	    public void should_add_the_commit_to_the_set_of_undispatched_commits() {
			Persistence.GetUndispatchedCommits()
				.FirstOrDefault(x => x.CommitId == attempt.CommitId).ShouldNotBeNull();
        }

	    public void should_cause_the_stream_to_be_found_in_the_list_of_streams_to_snapshot()
	    {
	        Persistence.GetStreamsToSnapshot(1)
	                   .FirstOrDefault(x => x.StreamId == streamId).ShouldNotBeNull();
	    }
	}

	public class when_reading_from_a_given_revision : PersistenceEngineConcern
        
	{
        Guid streamId;

		const int LoadFromCommitContainingRevision = 3;
		const int UpToCommitWithContainingRevision = 5;
	    Commit oldest, oldest2, oldest3;
		Commit[] committed;

        protected override void Context()
        {
            oldest = Persistence.CommitSingle(); // 2 events, revision 1-2
            oldest2 = Persistence.CommitNext(oldest); // 2 events, revision 3-4
            oldest3 = Persistence.CommitNext(oldest2); // 2 events, revision 5-6
            Persistence.CommitNext(oldest3); // 2 events, revision 7-8

            streamId = oldest.StreamId;
		}

	    protected override void Because()
	    {
	        committed = Persistence
                .GetFrom(streamId, LoadFromCommitContainingRevision, UpToCommitWithContainingRevision)
                .ToArray();
	    }

        [Fact]
        public void should_start_from_the_commit_which_contains_the_min_stream_revision_specified()
	    {
	        committed.First().CommitId.ShouldBe(oldest2.CommitId); // contains revision 3
	    }

        [Fact]
        public void should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified()
	    {
	        committed.Last().CommitId.ShouldBe(oldest3.CommitId); // contains revision 5
	    }
	}

    public class when_committing_a_stream_with_the_same_revision : PersistenceEngineConcern
        
	{
		Commit attemptWithSameRevision;
		Exception thrown;

        protected override void Context()
        {
            var attempt = Persistence.CommitSingle();
            attemptWithSameRevision = attempt.StreamId.BuildAttempt();
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => Persistence.Commit(attemptWithSameRevision));
        }

        [Fact]
        public void should_throw_a_ConcurrencyException()
        {
            thrown.ShouldBeInstanceOf<ConcurrencyException>();
        }
	}

    //TODO:This test looks exactly like the one above. What are we trying to prove?
    public class when_committing_a_stream_with_the_same_sequence : PersistenceEngineConcern
        
	{
		Commit attempt1, attempt2;
		Exception thrown;

        protected override void Context()
        {
            var streamId = Guid.NewGuid();
            attempt1 = streamId.BuildAttempt();
            attempt2 = streamId.BuildAttempt();

            Persistence.Commit(attempt1);
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => Persistence.Commit(attempt2));
        }

        [Fact]
        public void should_throw_a_ConcurrencyException()
        {
            thrown.ShouldBeInstanceOf<ConcurrencyException>();
        }
	}

    //TODO:This test looks exactly like the one above. What are we trying to prove?
    public class when_attempting_to_overwrite_a_committed_sequence : PersistenceEngineConcern
        
	{
		Exception thrown;
        Commit failedAttempt;

        protected override void Context()
        {
            var streamId = Guid.NewGuid();
            var successfulAttempt = streamId.BuildAttempt();
            failedAttempt = streamId.BuildAttempt();

            Persistence.Commit(successfulAttempt);
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => Persistence.Commit(failedAttempt));
        }

        [Fact]
        public void should_throw_a_ConcurrencyException()
        {
            thrown.ShouldBeInstanceOf<ConcurrencyException>();
        }
	}

    public class when_attempting_to_persist_a_commit_twice : PersistenceEngineConcern
        
    {
        Commit attemptTwice;
		Exception thrown;

        protected override void Context()
        {
            attemptTwice = Persistence.CommitSingle();
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => Persistence.Commit(attemptTwice));
        }

        [Fact]
        public void should_throw_a_DuplicateCommitException()
        {
            thrown.ShouldBeInstanceOf<DuplicateCommitException>();
        }
	}

    public class when_a_commit_has_been_marked_as_dispatched : PersistenceEngineConcern
        
	{
		Commit attempt;

        protected override void Context()
        {
            attempt = Persistence.CommitSingle();
        }

        protected override void Because()
        {
            Persistence.MarkCommitAsDispatched(attempt);
        }

        [Fact]
        public void should_no_longer_be_found_in_the_set_of_undispatched_commits()
        {
            Persistence.GetUndispatchedCommits()
                .FirstOrDefault(x => x.CommitId == attempt.CommitId)
                .ShouldBeNull();
        }
	}

    public class when_committing_more_events_than_the_configured_page_size : PersistenceEngineConcern
        
	{
        Guid streamId;
		HashSet<Guid> committed;
		ICollection<Guid> loaded;

        protected override void Context() {
            streamId = Guid.NewGuid();
            
            committed = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1, streamId)
                .Select(c => c.CommitId)
                .ToHashSet();
		}

        protected override void Because()
        {
            loaded = Persistence.GetFrom(streamId, 0, int.MaxValue)
                .Select(c => c.CommitId)
                .ToLinkedList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
        {
            loaded.Count.ShouldBe(committed.Count);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            committed.All(x => loaded.Contains(x)).ShouldBeTrue();
            // all commits should be found in loaded collection
        }
	}

    public class when_saving_a_snapshot : PersistenceEngineConcern
        
	{
		bool added;
        Snapshot snapshot;
        Guid streamId;

        protected override void Context()
        {
            streamId = Guid.NewGuid();
            snapshot = new Snapshot(streamId, 1, "Snapshot");
            Persistence.CommitSingle(streamId);
        }

        protected override void Because()
        {
            added = Persistence.AddSnapshot(snapshot);
        }

        [Fact]
        public void should_indicate_the_snapshot_was_added()
        {
            added.ShouldBeTrue();
        }

        [Fact]
        public void should_be_able_to_retrieve_the_snapshot()
        {
            Persistence.GetSnapshot(streamId, snapshot.StreamRevision).ShouldNotBeNull();
        }
	}

    public class when_retrieving_a_snapshot : PersistenceEngineConcern
        
	{
        Guid streamId;
		
        Snapshot tooFarBack, 
            correct, 
            tooFarForward, 
            snapshot;

        protected override void Context() {
            streamId = Guid.NewGuid();
			var commit1 = Persistence.CommitSingle(streamId); // rev 1-2
			var commit2 = Persistence.CommitNext(commit1); // rev 3-4
			Persistence.CommitNext(commit2); // rev 5-6

            Persistence.AddSnapshot(tooFarBack = new Snapshot(streamId, 1, string.Empty));
            Persistence.AddSnapshot(correct = new Snapshot(streamId, 3, "Snapshot"));
            Persistence.AddSnapshot(tooFarForward = new Snapshot(streamId, 5, string.Empty));
		}

        protected override void Because()
        {
            snapshot = Persistence.GetSnapshot(streamId, tooFarForward.StreamRevision - 1);
        }

        [Fact]
        public void should_load_the_most_recent_prior_snapshot()
        {
            snapshot.StreamRevision.ShouldBe(correct.StreamRevision);
        }

        [Fact]
        public void should_have_the_correct_snapshot_payload()
        {
            snapshot.Payload.ShouldBe(correct.Payload);
        }
	}

    public class when_a_snapshot_has_been_added_to_the_most_recent_commit_of_a_stream : PersistenceEngineConcern
        
	{
		const string SnapshotData = "snapshot";
	    Guid streamId;
		Commit oldest, oldest2, newest;

	    protected override void Context() 
        {
            streamId = Guid.NewGuid();
			oldest = Persistence.CommitSingle(streamId);
			oldest2 = Persistence.CommitNext(oldest);
			newest = Persistence.CommitNext(oldest2);
		}

	    protected override void Because()
	    {
	        Persistence.AddSnapshot(new Snapshot(streamId, newest.StreamRevision, SnapshotData));
	    }

        [Fact]
        public void should_no_longer_find_the_stream_in_the_set_of_streams_to_be_snapshot()
	    {
	        Persistence.GetStreamsToSnapshot(1).Any(x => x.StreamId == streamId).ShouldBeFalse();
	    }
	}

    public class when_adding_a_commit_after_a_snapshot : PersistenceEngineConcern
        
	{
		const int WithinThreshold = 2;
		const int OverThreshold = 3;
		const string SnapshotData = "snapshot";
        Guid streamId;
		
        Commit oldest, oldest2;

        protected override void Context()
		{
            streamId = Guid.NewGuid();
			oldest = Persistence.CommitSingle(streamId);
			oldest2 = Persistence.CommitNext(oldest);
			Persistence.AddSnapshot(new Snapshot(streamId, oldest2.StreamRevision, SnapshotData));
		}

        protected override void Because()
        {
            Persistence.Commit(oldest2.BuildNextAttempt());
        }

        // Because Raven and Mongo update the stream head asynchronously, occasionally will fail this test
        [Fact]
        public void should_find_the_stream_in_the_set_of_streams_to_be_snapshot_when_within_the_threshold()
        {
            Persistence.GetStreamsToSnapshot(WithinThreshold)
                .FirstOrDefault(x => x.StreamId == streamId)
                .ShouldNotBeNull();
        }

        [Fact]
        public void should_not_find_the_stream_in_the_set_of_streams_to_be_snapshot_when_over_the_threshold()
        {
            Persistence.GetStreamsToSnapshot(OverThreshold)
                .Any(x => x.StreamId == streamId)
                .ShouldBeFalse();
        }
	}

    public class when_reading_all_commits_from_a_particular_point_in_time : PersistenceEngineConcern
        
	{
        Guid streamId;
        DateTime now;
        Commit first, second, third;
		Commit[] committed;

        protected override void Context()
		{
            streamId = Guid.NewGuid();

            now = SystemTime.UtcNow.AddYears(1);
		    first = streamId.BuildAttempt(now.AddSeconds(1));
            Persistence.Commit(first);

			second = Persistence.CommitNext(first);
			third = Persistence.CommitNext(second);
			Persistence.CommitNext(third);
		}

        protected override void Because()
        {
            committed = Persistence.GetFrom(now).ToArray();
        }

        [Fact]
        public void should_return_all_commits_on_or_after_the_point_in_time_specified()
        {
            committed.Length.ShouldBe(4);
        }
	}

    public class when_paging_over_all_commits_from_a_particular_point_in_time : PersistenceEngineConcern
        
	{
        Guid streamId;
		DateTime start;
		HashSet<Guid> committed;
		ICollection<Guid> loaded;

        protected override void Context()
        {
            start = SystemTime.UtcNow;
            committed = Persistence.CommitMany(ConfiguredPageSizeForTesting + 1)
                .Select(c => c.CommitId)
                .ToHashSet();
		}

	    protected override void Because()
	    {
	        loaded = Persistence.GetFrom(start)
                .Select(c => c.CommitId)
                .ToLinkedList();
	    }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
	    {
	        loaded.Count.ShouldBeGreaterThanOrEqualTo(committed.Count); // >= because items may be loaded from other tests.
	    }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
	    {
	        committed.All(x => loaded.Contains(x)).ShouldBeTrue(); // all commits should be found in loaded collection
	    }
	}

    public class when_reading_all_commits_from_the_year_1_AD : PersistenceEngineConcern
        
	{
		Exception thrown;

        protected override void Because()
        {
            thrown = Catch.Exception(() => Persistence.GetFrom(DateTime.MinValue).FirstOrDefault());
        }

        [Fact]
        public void should_NOT_throw_an_exception()
        {
            thrown.ShouldBeNull();
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
		Exception thrown;

		protected override void Context()
	    {
	        Persistence.Dispose();
	    }

        protected override void Because()
        {
            thrown = Catch.Exception(() => Persistence.CommitSingle());
        }

        [Fact]
        public void should_throw_an_ObjectDisposedException()
        {
			thrown.ShouldBeInstanceOf<ObjectDisposedException>();
        }
	}

    public class when_persisting_commits_out_of_order : PersistenceEngineConcern
    {
        // Issue 159 OrderingByCommitStampIsNotReliable
        Commit[] undispatched;

        protected override void Context()
        {
            Persistence.Purge();
            var streamId = Guid.NewGuid();
            var dateTime = new DateTime(2013, 1, 1);
            SystemTime.Resolver = () => dateTime;
            Persistence.Commit(new Commit(streamId, 1, Guid.NewGuid(), 1, SystemTime.UtcNow, null, new List<EventMessage> { new EventMessage{ Body = "M1" } }));
            Persistence.Commit(new Commit(streamId, 3, Guid.NewGuid(), 3, SystemTime.UtcNow, null, new List<EventMessage> { new EventMessage { Body = "M3" } }));
            Persistence.Commit(new Commit(streamId, 2, Guid.NewGuid(), 2, SystemTime.UtcNow, null, new List<EventMessage> { new EventMessage { Body = "M2" } }));
        }

        protected override void Because()
        {
            undispatched = Persistence.GetUndispatchedCommits().ToArray();
        }

        protected override void Cleanup()
        {
            SystemTime.Resolver = null;
        }

        [Fact]
        public void should_have_commits_in_correct_order()
        {
            for (var i = 1; i <= undispatched.Length; i++)
            {
                undispatched[i-1].CommitSequence.ShouldBe(i);
            }
        }
    }

    public partial class PersistenceEngineConcern : SpecificationBase, IUseFixture<PersistenceEngineFixture>
    {
        PersistenceEngineFixture data;

        public IPersistStreams Persistence { get { return data.Persistence; } }

        public virtual int ConfiguredPageSizeForTesting
        {
            get { return int.Parse("pageSize".GetSetting() ?? "0"); }
        }

        public void SetFixture(PersistenceEngineFixture data)
        {
            this.data = data;
        }
    }

    public partial class PersistenceEngineFixture : IDisposable
    {
        IPersistStreams persistence;
        public Func<IPersistStreams> CreatePersistence { get; set; }

        public IPersistStreams Persistence
        {
            get
            {
                if (persistence == null)
                {
                    persistence = new PerformanceCounterPersistenceEngine(CreatePersistence(), "tests");
                    persistence.Initialize();
                }

                return persistence;
            }
        }

        protected bool PurgeOnDispose { get; set; }

        public void Dispose()
        {
            if(persistence != null && PurgeOnDispose)
                persistence.Purge();

            Persistence.Dispose();
        }
    }
}