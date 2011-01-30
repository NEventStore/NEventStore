using System.Threading;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Linq;
	using Machine.Specifications;
	using Persistence;

	[Subject("Persistence")]
	public class when_a_commit_is_successfully_persisted : using_the_persistence_engine
	{
		static readonly Commit attempt = streamId.BuildAttempt();

		Because of = () =>
			persistence.Commit(attempt);

		It should_make_the_commit_available_to_be_read_from_the_stream = () =>
			persistence.GetFrom(streamId, 0, int.MaxValue).First().CommitId.ShouldEqual(attempt.CommitId);

		It should_add_the_commit_to_the_set_of_undispatched_commits = () =>
			persistence.GetUndispatchedCommits().FirstOrDefault(x => x.CommitId == attempt.CommitId).ShouldNotBeNull();

		It should_cause_the_stream_to_be_found_in_the_list_of_streams_to_snapshot = () =>
			persistence.GetStreamsToSnapshot(1).First(x => x.StreamId == streamId).ShouldNotBeNull();

		It should_serialize_and_deserialize_the_events_correctly = () =>
			persistence.GetFrom(streamId, 0, int.MaxValue)
				.Select(c => c.Events.First().Body as ExtensionMethods.SomeDomainEvent)
				.First().SomeProperty.ShouldEqual("Test");
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
	public class when_committing_a_stream_with_the_same_revision_it_should_throw_a_concurrency_exceptuon : using_the_persistence_engine
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

		It should_throw_a_concurrency_exception = () => thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("Persistence")]
	public class when_committing_a_stream_with_the_same_sequence_it_should_throw_a_concurrency_exception : using_the_persistence_engine
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

		It should_throw_a_concurrency_exception = () => thrown.ShouldBeOfType<ConcurrencyException>();
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
			persistence.GetUndispatchedCommits().FirstOrDefault(x => x.CommitId == attempt.CommitId).ShouldBeNull();
	}

	[Subject("Persistence")]
	public class when_saving_a_snapshot : using_the_persistence_engine
	{
		static readonly Snapshot snapshot = new Snapshot(streamId, 1, "Snapshot");
		static bool added;

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
		static readonly Snapshot tooFarForward = new Snapshot(streamId, 100, string.Empty);
		static Snapshot snapshot;

		Establish context = () =>
		{
			persistence.AddSnapshot(tooFarBack);
			persistence.AddSnapshot(correct);
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
	public class when_reading_all_commits_from_a_particular_point_in_time : using_the_persistence_engine
	{
		static readonly DateTime start = DateTime.UtcNow.AddMilliseconds(10);
		static readonly Commit first = streamId.BuildAttempt();
		static readonly Commit second = first.BuildNextAttempt();
		static readonly Commit third = second.BuildNextAttempt();
		static readonly Commit fourth = third.BuildNextAttempt();
		static Commit[] committed;

		Establish context = () =>
		{
			Thread.Sleep(10);
			persistence.Commit(first);
			persistence.Commit(second);
			persistence.Commit(third);
			persistence.Commit(fourth);
        };

		Because of = () =>
			committed = persistence.GetFrom(start).ToArray();

		It should_return_all_commits_on_or_after_the_point_in_time_specified = () =>
			committed.Length.ShouldEqual(4);
	}

	public abstract class using_the_persistence_engine
	{
		protected static readonly IPersistenceFactory Factory = new PersistenceFactoryScanner().GetFactory();
		protected static Guid streamId = Guid.NewGuid();
		protected static IPersistStreams persistence;

		Establish context = () =>
		{
			persistence = Factory.Build();
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