#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using System.Linq;
	using Machine.Specifications;
	using EventStore.Persistence;
	using It = Machine.Specifications.It;

	[Subject("OptimisticCommitHook")]
	public class when_committing_with_a_sequence_beyond_the_known_end_of_a_stream : using_commit_hooks
	{
		const int HeadStreamRevision = 5;
		const int HeadCommitSequence = 1;
		const int ExpectedNextCommitSequence = HeadCommitSequence + 1;
		const int BeyondEndOfStreamCommitSequence = ExpectedNextCommitSequence + 1;
		static readonly Commit beyondEndOfStream = BuildCommitStub(HeadStreamRevision + 1, BeyondEndOfStreamCommitSequence);
		static readonly Commit alreadyCommitted = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
		static Exception thrown;

		Establish context = () =>
			hook.PostCommit(alreadyCommitted);

		Because of = () =>
			thrown = Catch.Exception(() => hook.PreCommit(beyondEndOfStream));

		It should_throw_a_PersistenceException = () =>
			thrown.ShouldBeOfType<StorageException>();
	}

	[Subject("OptimisticCommitHook")]
	public class when_committing_with_a_revision_beyond_the_known_end_of_a_stream : using_commit_hooks
	{
		const int HeadCommitSequence = 1;
		const int HeadStreamRevision = 1;
		const int NumberOfEventsBeingCommitted = 1;
		const int ExpectedNextStreamRevision = HeadStreamRevision + 1 + NumberOfEventsBeingCommitted;
		const int BeyondEndOfStreamRevision = ExpectedNextStreamRevision + 1;
		static readonly Commit alreadyCommitted = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
		static readonly Commit beyondEndOfStream = BuildCommitStub(BeyondEndOfStreamRevision, HeadCommitSequence + 1);
		static Exception thrown;

		Establish context = () =>
			hook.PostCommit(alreadyCommitted);

		Because of = () =>
			thrown = Catch.Exception(() => hook.PreCommit(beyondEndOfStream));

		It should_throw_a_PersistenceException = () =>
			thrown.ShouldBeOfType<StorageException>();
	}

	[Subject("OptimisticCommitHook")]
	public class when_committing_with_a_sequence_less_or_equal_to_the_most_recent_sequence_for_the_stream : using_commit_hooks
	{
		const int HeadStreamRevision = 42;
		const int HeadCommitSequence = 42;
		const int DupliateCommitSequence = HeadCommitSequence;
		static readonly Commit Committed = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
		static readonly Commit Attempt = BuildCommitStub(HeadStreamRevision + 1, DupliateCommitSequence);

		static Exception thrown;

		Establish context = () =>
			hook.PostCommit(Committed);

		Because of = () =>
			thrown = Catch.Exception(() => hook.PreCommit(Attempt));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("OptimisticCommitHook")]
	public class when_committing_with_a_revision_less_or_equal_to_than_the_most_recent_revision_read_for_the_stream : using_commit_hooks
	{
		const int HeadStreamRevision = 3;
		const int HeadCommitSequence = 2;
		const int DuplicateStreamRevision = HeadStreamRevision;
		static readonly Commit Committed = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
		static readonly Commit FailedAttempt = BuildCommitStub(DuplicateStreamRevision, HeadCommitSequence + 1);
		static Exception thrown;

		Establish context = () =>
			hook.PostCommit(Committed);

		Because of = () =>
			thrown = Catch.Exception(() => hook.PreCommit(FailedAttempt));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("OptimisticCommitHook")]
	public class when_committing_with_a_commit_sequence_less_than_or_equal_to_the_most_recent_commit_for_the_stream : using_commit_hooks
	{
		const int DuplicateCommitSequence = 1;

		static readonly Commit SuccessfulAttempt = BuildCommitStub(1, DuplicateCommitSequence);
		static readonly Commit FailedAttempt = BuildCommitStub(2, DuplicateCommitSequence);
		static Exception thrown;

		Establish context = () =>
			hook.PostCommit(SuccessfulAttempt);

		Because of = () =>
			thrown = Catch.Exception(() => hook.PreCommit(FailedAttempt));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("OptimisticCommitHook")]
	public class when_committing_with_a_stream_revision_less_than_or_equal_to_the_most_recent_commit_for_the_stream : using_commit_hooks
	{
		const int DuplicateStreamRevision = 2;

		static readonly Commit SuccessfulAttempt = BuildCommitStub(DuplicateStreamRevision, 1);
		static readonly Commit FailedAttempt = BuildCommitStub(DuplicateStreamRevision, 2);
		static Exception thrown;

		Establish context = () =>
			hook.PostCommit(SuccessfulAttempt);

		Because of = () =>
			thrown = Catch.Exception(() => hook.PreCommit(FailedAttempt));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("CommitTracker")]
	public class when_tracking_commits
	{
		const int MaxStreamsToTrack = 2;
		static readonly Guid StreamId = Guid.NewGuid();
		static readonly Commit[] TrackedCommits = new[]
		{
			BuildCommit(Guid.NewGuid(), Guid.NewGuid()),
			BuildCommit(Guid.NewGuid(), Guid.NewGuid()),
			BuildCommit(Guid.NewGuid(), Guid.NewGuid())
		};

		static OptimisticPipelineHook hook;

		Establish context = () =>
			hook = new OptimisticPipelineHook(MaxStreamsToTrack);

		Because of = () =>
		{
			foreach (var commit in TrackedCommits)
				hook.Track(commit);
		};

		It should_only_contain_streams_explicitly_tracked = () =>
		{
			var untracked = BuildCommit(Guid.Empty, TrackedCommits[0].CommitId);
			hook.Contains(untracked).ShouldBeFalse();
		};

		It should_find_tracked_streams = () =>
		{
			var stillTracked = BuildCommit(TrackedCommits.Last().StreamId, TrackedCommits.Last().CommitId);
			hook.Contains(stillTracked).ShouldBeTrue();
		};

		It should_only_track_the_specified_number_of_streams = () =>
		{
			var droppedFromTracking = BuildCommit(
				TrackedCommits.First().StreamId, TrackedCommits.First().CommitId);
			hook.Contains(droppedFromTracking).ShouldBeFalse();
		};

		private static Commit BuildCommit(Guid streamId, Guid commitId)
		{
			return new Commit(streamId, 0, commitId, 0, SystemTime.UtcNow, null, null);
		}
	}

	public abstract class using_commit_hooks
	{
		protected static Guid streamId = Guid.NewGuid();
		protected static OptimisticPipelineHook hook;

		Establish context = () =>
		{
			streamId = Guid.NewGuid();
			hook = new OptimisticPipelineHook();
		};

		Cleanup everything = () =>
			streamId = Guid.NewGuid();

		protected static Commit BuildCommitStub(Guid commitId)
		{
			return new Commit(streamId, 1, commitId, 1, SystemTime.UtcNow, null, null);
		}
		protected static Commit BuildCommitStub(int streamRevision, int commitSequence)
		{
			var events = new[] { new EventMessage() }.ToList();
			return new Commit(streamId, streamRevision, Guid.NewGuid(), commitSequence, SystemTime.UtcNow, null, events);
		}
		protected static Commit BuildCommitStub(Guid commitId, int streamRevision, int commitSequence)
		{
			var events = new[] { new EventMessage() }.ToList();
			return new Commit(streamId, streamRevision, commitId, commitSequence, SystemTime.UtcNow, null, events);
		}
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169