#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Persistence.AcceptanceTests
{
	using System;
	using System.Linq;
	using Machine.Specifications;

	[Subject("Persistence")]
	public class when_an_attempt_is_successfully_committed : using_the_persistence_engine
	{
		static readonly CommitAttempt attempt = streamId.BuildAttempt();

		Because of = () =>
			persistence.Persist(attempt);

		It should_make_the_commit_available_to_be_read_from_the_stream = () =>
			persistence.GetFrom(streamId, 0).First().CommitId.ShouldEqual(attempt.CommitId);

		It should_add_the_commit_to_the_set_of_undispatched_commits = () =>
			persistence.GetUndispatchedCommits().FirstOrDefault(x => x.CommitId == attempt.CommitId).ShouldNotBeNull();

		It should_serialize_and_deserialize_the_events_correctly = () => persistence.GetFromSnapshotUntil(streamId, int.MaxValue)
		                                                                 	.Select(c => c.Events.First().Body as ExtensionMethods.SomeDomainEvent)
		                                                                 	.First().SomeProperty.ShouldEqual("Test");

	}

	[Subject("Persistence")]
	public class when_a_commit_has_been_marked_as_dispatched : using_the_persistence_engine
	{
		static readonly CommitAttempt attempt = streamId.BuildAttempt();

		Establish context = () =>
			persistence.Persist(attempt);

		Because of = () =>
			persistence.MarkCommitAsDispatched(attempt.ToCommit());

		It should_no_longer_be_found_in_the_set_of_undispatched_commits = () =>
			persistence.GetUndispatchedCommits().FirstOrDefault(x => x.CommitId == attempt.CommitId).ShouldBeNull();
	}

	[Subject("Persistence")]
	public class when_a_snapshot_has_been_added_to_the_most_recent_commit : using_the_persistence_engine
	{
		const string SnapshotData = "snapshot";
		static readonly CommitAttempt oldest = streamId.BuildAttempt();
		static readonly CommitAttempt oldest2 = oldest.BuildNextAttempt();
		static readonly CommitAttempt newest = oldest2.BuildNextAttempt();
		static readonly Commit head = newest.ToCommit();

		Establish context = () =>
		{
			persistence.Persist(oldest);
			persistence.Persist(oldest2);
			persistence.Persist(newest);
		};

		Because of = () =>
			persistence.AddSnapshot(streamId, head.StreamRevision, SnapshotData);

		It should_start_reads_at_the_most_recent_commit_prior_to_the_revision_specified = () =>
			persistence.GetFromSnapshotUntil(streamId, head.StreamRevision).First().CommitId.ShouldEqual(newest.CommitId);

		It should_be_able_to_read_prior_to_the_snapshot_revision = () =>
			persistence.GetFromSnapshotUntil(streamId, oldest2.ToCommit().StreamRevision).Last().CommitId.ShouldEqual(oldest2.CommitId);

		It should_set_the_snapshot_on_the_commit = () =>
			persistence.GetFromSnapshotUntil(streamId, head.StreamRevision).First().Snapshot.ShouldEqual(SnapshotData);

		It should_no_longer_find_the_commit_in_the_set_of_streams_to_be_snapshot = () =>
			persistence.GetStreamsToSnapshot(1).Any(x => x.StreamId == streamId).ShouldBeFalse();
	}

	[Subject("Persistence")]
	public class when_reading_from_a_given_revision : using_the_persistence_engine
	{
		private const int LoadFromCommitContainingRevision = 3;
		static readonly CommitAttempt oldest = streamId.BuildAttempt(); // 2 events, revision 1-2
		static readonly CommitAttempt oldest2 = oldest.BuildNextAttempt(); // 2 events, revision 3-4
		static readonly CommitAttempt oldest3 = oldest2.BuildNextAttempt(); // 2 events, revision 5-6
		static readonly CommitAttempt newest = oldest3.BuildNextAttempt(); // 2 events, revision 7-8
		static Commit[] committed;

		Establish context = () =>
		{
			persistence.Persist(oldest);
			persistence.Persist(oldest2);
			persistence.Persist(oldest3);
			persistence.Persist(newest);
		};

		Because of = () =>
			committed = persistence.GetFrom(streamId, LoadFromCommitContainingRevision).ToArray();

		It should_start_from_the_commit_which_contains_the_given_stream_revision = () =>
			committed.First().CommitId.ShouldEqual(oldest2.CommitId);

		It should_read_up_to_the_end_of_the_stream = () =>
			committed.Last().CommitId.ShouldEqual(newest.CommitId);
	}

	[Subject("Persistence")]
	public class when_reading_until_a_given_revision : using_the_persistence_engine
	{
		private const int LoadUpToCommitWhichContainsRevision = 7;
		static readonly CommitAttempt oldest = streamId.BuildAttempt(); // 2 events, revision 1-2
		static readonly CommitAttempt oldest2 = oldest.BuildNextAttempt(); // 2 events, revision 3-4
		static readonly CommitAttempt oldest3 = oldest2.BuildNextAttempt(); // 2 events, revision 5-6
		static readonly CommitAttempt oldest4 = oldest3.BuildNextAttempt(); // 2 events, revision 7-8
		static readonly CommitAttempt newest = oldest4.BuildNextAttempt(); // 2 events, revision 9-10
		static Commit[] committed;

		Establish context = () =>
		{
			persistence.Persist(oldest);
			persistence.Persist(oldest2);
			persistence.Persist(oldest3);
			persistence.Persist(oldest4);
			persistence.Persist(newest);

			persistence.AddSnapshot(streamId, oldest2.StreamRevision, "snapshot");
		};

		Because of = () =>
			committed = persistence.GetFromSnapshotUntil(streamId, LoadUpToCommitWhichContainsRevision).ToArray();

		It should_start_from_the_commit_of_the_most_recent_snapshot_on_or_before_the_given_revision = () =>
			committed.First().StreamRevision.ShouldEqual(oldest2.ToCommit().StreamRevision);

		It should_read_up_to_the_commit_containing_the_given_revision = () =>
			committed.Last().StreamRevision.ShouldEqual(oldest4.ToCommit().StreamRevision);
	}

	[Subject("Persistence")]
	public class when_reading_until_a_given_revision_which_has_no_snapshot : using_the_persistence_engine
	{
		private const int LoadUpToCommitWhichContainsRevision = 5;
		static readonly CommitAttempt oldest = streamId.BuildAttempt(); // 2 events, revision 1-2
		static readonly CommitAttempt oldest2 = oldest.BuildNextAttempt(); // 2 events, revision 3-4
		static readonly CommitAttempt oldest3 = oldest2.BuildNextAttempt(); // 2 events, revision 5-6
		static readonly CommitAttempt newest = oldest3.BuildNextAttempt(); // 2 events, revision 7-8
		static Commit[] committed;

		Establish context = () =>
		{
			persistence.Persist(oldest);
			persistence.Persist(oldest2);
			persistence.Persist(oldest3);
			persistence.Persist(newest);
		};

		Because of = () =>
			committed = persistence.GetFromSnapshotUntil(streamId, LoadUpToCommitWhichContainsRevision).ToArray();

		It should_start_from_the_first_commit = () =>
			committed.First().StreamRevision.ShouldEqual(oldest.ToCommit().StreamRevision);

		It should_read_up_to_the_commit_containing_the_given_revision = () =>
			committed.Last().StreamRevision.ShouldEqual(oldest3.ToCommit().StreamRevision);
	}

	[Subject("Persistence")]
	public class when_attempting_to_overwrite_a_committed_sequence : using_the_persistence_engine
	{
		static readonly CommitAttempt successfulAttempt = streamId.BuildAttempt();
		static readonly CommitAttempt failedAttempt = streamId.BuildAttempt();
		static Exception thrown;

		Establish context = () =>
			persistence.Persist(successfulAttempt);

		Because of = () =>
			thrown = Catch.Exception(() => persistence.Persist(failedAttempt));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("Persistence")]
	public class when_reattempting_a_previously_committed_attempt : using_the_persistence_engine
	{
		static readonly CommitAttempt attemptTwice = streamId.BuildAttempt();
		static Exception thrown;

		Establish context = () =>
			persistence.Persist(attemptTwice);

		Because of = () =>
			thrown = Catch.Exception(() => persistence.Persist(attemptTwice));

		It should_throw_a_DuplicateCommitException = () =>
			thrown.ShouldBeOfType<DuplicateCommitException>();
	}

	[Subject("Persistence")]
	public class when_reading_all_commits_from_a_particular_point_in_time : using_the_persistence_engine
	{
		static readonly DateTime start = DateTime.UtcNow;
		static readonly CommitAttempt first = streamId.BuildAttempt();
		static readonly CommitAttempt second = first.BuildNextAttempt();
		static readonly CommitAttempt third = second.BuildNextAttempt();
		static readonly CommitAttempt fourth = third.BuildNextAttempt();
		static Commit[] committed;

		Establish context = () =>
		{
			persistence.Persist(first);
			persistence.Persist(second);
			persistence.Persist(third);
			persistence.Persist(fourth);
		};

		Because of = () =>
			committed = persistence.GetFrom(start).ToArray();

		It should_return_all_commits_on_or_after_the_point_in_time_specified = () =>
			committed.Length.ShouldEqual(4);
	}

	public abstract class using_the_persistence_engine
	{
		private static readonly IPersistenceFactory factory = new PersistenceFactoryScanner().GetFactory();
		protected static Guid streamId = Guid.NewGuid();
		protected static IPersistStreams persistence;

		Establish context = () =>
		{
			persistence = factory.Build();
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