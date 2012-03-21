using EventStore.Diagnostics;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using Machine.Specifications;
	using Persistence;

	[Subject("Persistence")]
	public class when_a_commit_is_successfully_persisted : using_the_persistence_engine
	{
		static readonly DateTime now = SystemTime.UtcNow.AddYears(1);
		static readonly Commit attempt = streamId.BuildAttempt(now);
		static Commit persisted;

		Establish context = () =>
			persistence.Commit(attempt);

		Because of = () =>
			persisted = persistence.GetFrom(streamId, 0, int.MaxValue).First();

		It should_correctly_persist_the_stream_identifier = () =>
			persisted.StreamId.ShouldEqual(attempt.StreamId);

		It should_correctly_persist_the_stream_stream_revision = () =>
			persisted.StreamRevision.ShouldEqual(attempt.StreamRevision);

		It should_correctly_persist_the_commit_identifier = () =>
			persisted.CommitId.ShouldEqual(attempt.CommitId);

		It should_correctly_persist_the_commit_sequence = () =>
			persisted.CommitSequence.ShouldEqual(attempt.CommitSequence);

		// persistence engines have varying levels of precision with respect to time.
		It should_correctly_persist_the_commit_stamp = () =>
			persisted.CommitStamp.Subtract(now).ShouldBeLessThan(TimeSpan.FromSeconds(1));

		It should_correctly_persist_the_headers = () =>
			persisted.Headers.Count.ShouldEqual(attempt.Headers.Count);

		It should_correctly_persist_the_events = () =>
			persisted.Events.Count.ShouldEqual(attempt.Events.Count);

		It should_add_the_commit_to_the_set_of_undispatched_commits = () =>
			persistence.GetUndispatchedCommits()
				.FirstOrDefault(x => x.CommitId == attempt.CommitId).ShouldNotBeNull();

		It should_cause_the_stream_to_be_found_in_the_list_of_streams_to_snapshot = () =>
			persistence.GetStreamsToSnapshot(1)
				.FirstOrDefault(x => x.StreamId == streamId).ShouldNotBeNull();
	}

	[Subject("Persistence")]
	public class when_reading_from_a_given_revision : using_the_persistence_engine
	{
		const int LoadFromCommitContainingRevision = 3;
		const int UpToCommitWithContainingRevision = 5;
		static readonly Commit oldest = streamId.BuildAttempt(); // 2 events, revision 1-2
		static readonly Commit oldest2 = oldest.BuildNextAttempt(); // 2 events, revision 3-4
		static readonly Commit oldest3 = oldest2.BuildNextAttempt(); // 2 events, revision 5-6
		static readonly Commit newest = oldest3.BuildNextAttempt(); // 2 events, revision 7-8
		static Commit[] committed;

		Establish context = () =>
		{
			persistence.Commit(oldest);
			persistence.Commit(oldest2);
			persistence.Commit(oldest3);
			persistence.Commit(newest);
		};

		Because of = () =>
			committed = persistence.GetFrom(streamId, LoadFromCommitContainingRevision, UpToCommitWithContainingRevision).ToArray();

		It should_start_from_the_commit_which_contains_the_min_stream_revision_specified = () =>
			committed.First().CommitId.ShouldEqual(oldest2.CommitId); // contains revision 3

		It should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified = () =>
			committed.Last().CommitId.ShouldEqual(oldest3.CommitId); // contains revision 5
	}

	[Subject("Persistence")]
	public class when_committing_a_stream_with_the_same_revision : using_the_persistence_engine
	{
		static readonly IPersistStreams persistence1 = Factory.Build();
		static readonly IPersistStreams persistence2 = Factory.Build();
		static readonly Commit attempt1 = streamId.BuildAttempt();
		static readonly Commit attempt2 = streamId.BuildAttempt();
		static Exception thrown;

		Establish context = () =>
			persistence1.Commit(attempt1);

		Because of = () =>
			thrown = Catch.Exception(() => persistence2.Commit(attempt2));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();

		Cleanup cleanup = () =>
		{
			persistence1.Dispose();
			persistence2.Dispose();
		};
	}

	[Subject("Persistence")]
	public class when_committing_a_stream_with_the_same_sequence : using_the_persistence_engine
	{
		static readonly IPersistStreams persistence1 = Factory.Build();
		static readonly IPersistStreams persistence2 = Factory.Build();
		static readonly Commit attempt1 = streamId.BuildAttempt();
		static readonly Commit attempt2 = streamId.BuildAttempt();
		static Exception thrown;

		Establish context = () =>
			persistence1.Commit(attempt1);

		Because of = () =>
			thrown = Catch.Exception(() => persistence2.Commit(attempt2));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();

		Cleanup cleanup = () =>
		{
			persistence1.Dispose();
			persistence2.Dispose();
		};
	}

	[Subject("Persistence")]
	public class when_attempting_to_overwrite_a_committed_sequence : using_the_persistence_engine
	{
		static readonly Commit successfulAttempt = streamId.BuildAttempt();
		static readonly Commit failedAttempt = streamId.BuildAttempt();
		static Exception thrown;

		Establish context = () =>
			persistence.Commit(successfulAttempt);

		Because of = () =>
			thrown = Catch.Exception(() => persistence.Commit(failedAttempt));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("Persistence")]
	public class when_attempting_to_persist_a_commit_twice : using_the_persistence_engine
	{
		static readonly Commit attemptTwice = streamId.BuildAttempt();
		static Exception thrown;

		Establish context = () =>
			persistence.Commit(attemptTwice);

		Because of = () =>
			thrown = Catch.Exception(() => persistence.Commit(attemptTwice));

		It should_throw_a_DuplicateCommitException = () =>
			thrown.ShouldBeOfType<DuplicateCommitException>();
	}

	[Subject("Persistence")]
	public class when_a_commit_has_been_marked_as_dispatched : using_the_persistence_engine
	{
		static readonly Commit attempt = streamId.BuildAttempt();

		Establish context = () =>
			persistence.Commit(attempt);

		Because of = () =>
			persistence.MarkCommitAsDispatched(attempt);

		It should_no_longer_be_found_in_the_set_of_undispatched_commits = () =>
			persistence.GetUndispatchedCommits()
				.FirstOrDefault(x => x.CommitId == attempt.CommitId).ShouldBeNull();
	}

	[Subject("Persistence")]
	public class when_committing_more_events_than_the_configured_page_size : using_the_persistence_engine
	{
		static readonly int ConfiguredPageSize = FactoryScanner.PageSize;
		static readonly int CommitsToPersist = ConfiguredPageSize + 1;
		static readonly HashSet<Guid> committed = new HashSet<Guid>();
		static readonly ICollection<Guid> loaded = new LinkedList<Guid>();
		static Commit attempt = streamId.BuildAttempt();

		Establish context = () =>
		{
			var attempt = streamId.BuildAttempt();
			for (var i = 0; i < CommitsToPersist; i++)
			{
				persistence.Commit(attempt);
				committed.Add(attempt.CommitId);
				attempt = attempt.BuildNextAttempt();
			}
		};

		Because of = () =>
			persistence.GetFrom(streamId, 0, int.MaxValue).ToList().ForEach(x => loaded.Add(x.CommitId));

		It should_load_the_same_number_of_commits_which_have_been_persisted = () =>
			loaded.Count.ShouldEqual(committed.Count);

		It should_load_the_same_commits_which_have_been_persisted = () =>
			committed.All(x => loaded.Contains(x)).ShouldBeTrue(); // all commits should be found in loaded collection
	}

	[Subject("Persistence")]
	public class when_saving_a_snapshot : using_the_persistence_engine
	{
		static readonly Snapshot snapshot = new Snapshot(streamId, 1, "Snapshot");
		static bool added;

		Establish context = () =>
			persistence.Commit(streamId.BuildAttempt());

		Because of = () =>
			added = persistence.AddSnapshot(snapshot);

		It should_indicate_the_snapshot_was_added = () =>
			added.ShouldBeTrue();

		It should_be_able_to_retrieve_the_snapshot = () =>
			persistence.GetSnapshot(streamId, snapshot.StreamRevision).ShouldNotBeNull();
	}

	[Subject("Persistence")]
	public class when_retrieving_a_snapshot : using_the_persistence_engine
	{
		static readonly Snapshot tooFarBack = new Snapshot(streamId, 1, string.Empty);
		static readonly Snapshot correct = new Snapshot(streamId, 3, "Snapshot");
		static readonly Snapshot tooFarForward = new Snapshot(streamId, 5, string.Empty);
		static Snapshot snapshot;

		Establish context = () =>
		{
			var commit1 = streamId.BuildAttempt();
			var commit2 = commit1.BuildNextAttempt();
			var commit3 = commit2.BuildNextAttempt();
			persistence.Commit(commit1); // rev 1-2
			persistence.Commit(commit2); // rev 3-4
			persistence.Commit(commit3); // rev 5-6

			persistence.AddSnapshot(tooFarBack);
			persistence.AddSnapshot(correct);
			persistence.AddSnapshot(tooFarForward);
		};

		Because of = () =>
			snapshot = persistence.GetSnapshot(streamId, tooFarForward.StreamRevision - 1);

		It should_load_the_most_recent_prior_snapshot = () =>
			snapshot.StreamRevision.ShouldEqual(correct.StreamRevision);

		It should_have_the_correct_snapshot_payload = () =>
			snapshot.Payload.ShouldEqual(correct.Payload);
	}

	[Subject("Persistence")]
	public class when_a_snapshot_has_been_added_to_the_most_recent_commit_of_a_stream : using_the_persistence_engine
	{
		const string SnapshotData = "snapshot";
		static readonly Commit oldest = streamId.BuildAttempt();
		static readonly Commit oldest2 = oldest.BuildNextAttempt();
		static readonly Commit newest = oldest2.BuildNextAttempt();

		Establish context = () =>
		{
			persistence.Commit(oldest);
			persistence.Commit(oldest2);
			persistence.Commit(newest);
		};

		Because of = () =>
			persistence.AddSnapshot(new Snapshot(streamId, newest.StreamRevision, SnapshotData));

		It should_no_longer_find_the_stream_in_the_set_of_streams_to_be_snapshot = () =>
			persistence.GetStreamsToSnapshot(1).Any(x => x.StreamId == streamId).ShouldBeFalse();
	}

	[Subject("Persistence")]
	public class when_adding_a_commit_after_a_snapshot : using_the_persistence_engine
	{
		const int WithinThreshold = 2;
		const int OverThreshold = 3;
		const string SnapshotData = "snapshot";
		static readonly Commit oldest = streamId.BuildAttempt();
		static readonly Commit oldest2 = oldest.BuildNextAttempt();
		static readonly Commit newest = oldest2.BuildNextAttempt();

		Establish context = () =>
		{
			persistence.Commit(oldest);
			persistence.Commit(oldest2);
			persistence.AddSnapshot(new Snapshot(streamId, oldest2.StreamRevision, SnapshotData));
		};

		Because of = () =>
			persistence.Commit(newest);

		// Because Raven and Mongo update the stream head asynchronously, occasionally will fail this test
		It should_find_the_stream_in_the_set_of_streams_to_be_snapshot_when_within_the_threshold = () =>
			persistence.GetStreamsToSnapshot(WithinThreshold)
				.FirstOrDefault(x => x.StreamId == streamId).ShouldNotBeNull();

		It should_not_find_the_stream_in_the_set_of_streams_to_be_snapshot_when_over_the_threshold = () =>
			persistence.GetStreamsToSnapshot(OverThreshold)
				.Any(x => x.StreamId == streamId).ShouldBeFalse();
	}

	[Subject("Persistence")]
	public class when_reading_all_commits_from_a_particular_point_in_time : using_the_persistence_engine
	{
		static readonly DateTime now = SystemTime.UtcNow.AddYears(1);
		static readonly Commit first = streamId.BuildAttempt(now.AddSeconds(1));
		static readonly Commit second = first.BuildNextAttempt();
		static readonly Commit third = second.BuildNextAttempt();
		static readonly Commit fourth = third.BuildNextAttempt();
		static Commit[] committed;

		Establish context = () =>
		{
			persistence.Commit(first);
			persistence.Commit(second);
			persistence.Commit(third);
			persistence.Commit(fourth);
		};

		Because of = () =>
			committed = persistence.GetFrom(now).ToArray();

		It should_return_all_commits_on_or_after_the_point_in_time_specified = () =>
			committed.Length.ShouldEqual(4);
	}

	[Subject("Persistence")]
	public class when_paging_over_all_commits_from_a_particular_point_in_time : using_the_persistence_engine
	{
		static readonly int ConfiguredPageSize = FactoryScanner.PageSize;
		static readonly int CommitsToPersist = ConfiguredPageSize + 1;
		static readonly DateTime start = SystemTime.UtcNow;
		static readonly HashSet<Guid> committed = new HashSet<Guid>();
		static readonly ICollection<Guid> loaded = new LinkedList<Guid>();
		static Commit attempt = streamId.BuildAttempt();

		Establish context = () =>
		{
			var attempt = streamId.BuildAttempt();
			for (var i = 0; i < CommitsToPersist; i++)
			{
				persistence.Commit(attempt);
				committed.Add(attempt.CommitId);
				attempt = attempt.BuildNextAttempt();
			}
		};

		Because of = () =>
			persistence.GetFrom(start).ToList().ForEach(x => loaded.Add(x.CommitId));

		It should_load_the_same_number_of_commits_which_have_been_persisted = () =>
			loaded.Count.ShouldBeGreaterThanOrEqualTo(committed.Count); // >= because items may be loaded from other tests.

		It should_load_the_same_commits_which_have_been_persisted = () =>
			committed.All(x => loaded.Contains(x)).ShouldBeTrue(); // all commits should be found in loaded collection
	}

	[Subject("Persistence")]
	public class when_reading_all_commits_from_the_year_1_AD : using_the_persistence_engine
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => persistence.GetFrom(DateTime.MinValue).FirstOrDefault());

		It should_NOT_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject("Persistence")]
	public class when_purging_all_commits : using_the_persistence_engine
	{
		Establish context = () =>
			persistence.Commit(streamId.BuildAttempt());

		Because of = () =>
		{
			Thread.Sleep(50); // 50 ms = enough time for Raven to become consistent
			persistence.Purge();
		};

		It should_not_find_any_commits_stored = () =>
			persistence.GetFrom(DateTime.MinValue).Count().ShouldEqual(0);

		It should_not_find_any_streams_to_snapshot = () =>
			persistence.GetStreamsToSnapshot(0).Count().ShouldEqual(0);

		It should_not_find_any_undispatched_commits = () =>
			persistence.GetUndispatchedCommits().Count().ShouldEqual(0);
	}

	[Subject("Persistence")]
	public class when_invoking_after_disposal : using_the_persistence_engine
	{
		static Exception thrown;

		Establish context = () =>
			persistence.Dispose();

		It should_throw_an_ObjectDisposedException = () =>
			Catch.Exception(() => persistence.Commit(streamId.BuildAttempt())).ShouldBeOfType<ObjectDisposedException>();
	}

	public abstract class using_the_persistence_engine
	{
		protected static readonly PersistenceFactoryScanner FactoryScanner = new PersistenceFactoryScanner();
		protected static readonly IPersistenceFactory Factory = FactoryScanner.GetFactory();
		protected static Guid streamId = Guid.NewGuid();
		protected static IPersistStreams persistence;

		Establish context = () =>
		{
			persistence = new PerformanceTrackingPersistenceDecorator(Factory.Build(), "tests");
			persistence.Initialize();

		};

		Cleanup everything = () =>
		{
			persistence.Dispose();
			persistence = null;
			streamId = Guid.NewGuid();
		};
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169