using EventStore.Persistence.AcceptanceTests;
using EventStore.Persistence.AcceptanceTests.BDD;
using Xunit;
using Xunit.Should;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using System.Linq;
	using EventStore.Persistence;

	public class when_committing_with_a_sequence_beyond_the_known_end_of_a_stream : using_commit_hooks
	{
		const int HeadStreamRevision = 5;
		const int HeadCommitSequence = 1;
		const int ExpectedNextCommitSequence = HeadCommitSequence + 1;
		const int BeyondEndOfStreamCommitSequence = ExpectedNextCommitSequence + 1;
		Commit beyondEndOfStream;
		Commit alreadyCommitted;
		Exception thrown;

	    protected override void Context()
	    {
            beyondEndOfStream = BuildCommitStub(HeadStreamRevision + 1, BeyondEndOfStreamCommitSequence);
		    alreadyCommitted = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);

	        hook.PostCommit(alreadyCommitted);
	    }

	    protected override void Because()
		{
		    thrown = Catch.Exception(() => hook.PreCommit(beyondEndOfStream));
		}

        [Fact]
        public void should_throw_a_PersistenceException()
		{
		    thrown.ShouldBeInstanceOf<StorageException>();
		}
	}

	public class when_committing_with_a_revision_beyond_the_known_end_of_a_stream : using_commit_hooks
	{
		const int HeadCommitSequence = 1;
		const int HeadStreamRevision = 1;
		const int NumberOfEventsBeingCommitted = 1;
		const int ExpectedNextStreamRevision = HeadStreamRevision + 1 + NumberOfEventsBeingCommitted;
		const int BeyondEndOfStreamRevision = ExpectedNextStreamRevision + 1;
	    Commit alreadyCommitted;
	    Commit beyondEndOfStream;
		Exception thrown;

		protected override void Context()
	    {
            alreadyCommitted = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
            beyondEndOfStream = BuildCommitStub(BeyondEndOfStreamRevision, HeadCommitSequence + 1);

	        hook.PostCommit(alreadyCommitted);
	    }

	    protected override void Because()
		{
		    thrown = Catch.Exception(() => hook.PreCommit(beyondEndOfStream));
		}

        [Fact]
        public void should_throw_a_PersistenceException()
		{
		    thrown.ShouldBeInstanceOf<StorageException>();
		}
	}

	public class when_committing_with_a_sequence_less_or_equal_to_the_most_recent_sequence_for_the_stream : using_commit_hooks
	{
		const int HeadStreamRevision = 42;
		const int HeadCommitSequence = 42;
		const int DupliateCommitSequence = HeadCommitSequence;
	    Commit Committed;
	    Commit Attempt;

		Exception thrown;

		protected override void Context()
	    {
            Committed = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
		    Attempt = BuildCommitStub(HeadStreamRevision + 1, DupliateCommitSequence);

	        hook.PostCommit(Committed);
	    }
        
        protected override void Because()
		{
		    thrown = Catch.Exception(() => hook.PreCommit(Attempt));
		}

        [Fact]
        public void should_throw_a_ConcurrencyException()
		{
		    thrown.ShouldBeInstanceOf<ConcurrencyException>();
		}
	}

	public class when_committing_with_a_revision_less_or_equal_to_than_the_most_recent_revision_read_for_the_stream : using_commit_hooks
	{
		const int HeadStreamRevision = 3;
		const int HeadCommitSequence = 2;
		const int DuplicateStreamRevision = HeadStreamRevision;
		Commit Committed;
	    Commit FailedAttempt;
		Exception thrown;

		protected override void Context()
	    {
            Committed = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
            FailedAttempt = BuildCommitStub(DuplicateStreamRevision, HeadCommitSequence + 1);

	        hook.PostCommit(Committed);
	    }

	    protected override void Because()
		{
		    thrown = Catch.Exception(() => hook.PreCommit(FailedAttempt));
		}

        [Fact]
        public void should_throw_a_ConcurrencyException()
		{
		    thrown.ShouldBeInstanceOf<ConcurrencyException>();
		}
	}

	public class when_committing_with_a_commit_sequence_less_than_or_equal_to_the_most_recent_commit_for_the_stream : using_commit_hooks
	{
		const int DuplicateCommitSequence = 1;

		Commit SuccessfulAttempt;
	    Commit FailedAttempt;
		Exception thrown;

		protected override void Context()
	    {
            SuccessfulAttempt = BuildCommitStub(1, DuplicateCommitSequence);
            FailedAttempt = BuildCommitStub(2, DuplicateCommitSequence);

	        hook.PostCommit(SuccessfulAttempt);
	    }

	    protected override void Because()
		{
		    thrown = Catch.Exception(() => hook.PreCommit(FailedAttempt));
		}

        [Fact]
        public void should_throw_a_ConcurrencyException()
		{
		    thrown.ShouldBeInstanceOf<ConcurrencyException>();
		}
	}

	public class when_committing_with_a_stream_revision_less_than_or_equal_to_the_most_recent_commit_for_the_stream : using_commit_hooks
	{
		const int DuplicateStreamRevision = 2;

	    Commit SuccessfulAttempt;
	    Commit FailedAttempt;
		Exception thrown;

		protected override void Context()
	    {
            SuccessfulAttempt = BuildCommitStub(DuplicateStreamRevision, 1);
            FailedAttempt = BuildCommitStub(DuplicateStreamRevision, 2);

	        hook.PostCommit(SuccessfulAttempt);
	    }

	    protected override void Because()
		{
		    thrown = Catch.Exception(() => hook.PreCommit(FailedAttempt));
		}

        [Fact]
        public void should_throw_a_ConcurrencyException()
		{
		    thrown.ShouldBeInstanceOf<ConcurrencyException>();
		}
	}

	public class when_tracking_commits : SpecificationBase
	{
		const int MaxStreamsToTrack = 2;
		readonly Guid StreamId = Guid.NewGuid();
		Commit[] TrackedCommits;

		OptimisticPipelineHook hook;

		protected override void Context()
	    {
            TrackedCommits = new[]
		    {
			    BuildCommit(Guid.NewGuid(), Guid.NewGuid()),
			    BuildCommit(Guid.NewGuid(), Guid.NewGuid()),
			    BuildCommit(Guid.NewGuid(), Guid.NewGuid())
		    };

	        hook = new OptimisticPipelineHook(MaxStreamsToTrack);
	    }
	
        protected override void Because()
		{
			foreach (var commit in TrackedCommits)
				hook.Track(commit);
		}

        [Fact]
        public void should_only_contain_streams_explicitly_tracked()
		{
			var untracked = BuildCommit(Guid.Empty, TrackedCommits[0].CommitId);
			hook.Contains(untracked).ShouldBeFalse();
		}

        [Fact]
        public void should_find_tracked_streams()
		{
			var stillTracked = BuildCommit(TrackedCommits.Last().StreamId, TrackedCommits.Last().CommitId);
			hook.Contains(stillTracked).ShouldBeTrue();
		}

        [Fact]
		public void should_only_track_the_specified_number_of_streams()
		{
			var droppedFromTracking = BuildCommit(
				TrackedCommits.First().StreamId, TrackedCommits.First().CommitId);
			hook.Contains(droppedFromTracking).ShouldBeFalse();
		}

		private Commit BuildCommit(Guid streamId, Guid commitId)
		{
			return new Commit(streamId, 0, commitId, 0, SystemTime.UtcNow, null, null);
		}
	}

	public abstract class using_commit_hooks : SpecificationBase
	{
        protected Guid streamId = Guid.NewGuid();
        protected OptimisticPipelineHook hook = new OptimisticPipelineHook();
        
	    protected Commit BuildCommitStub(Guid commitId)
		{
			return new Commit(streamId, 1, commitId, 1, SystemTime.UtcNow, null, null);
		}

		protected Commit BuildCommitStub(int streamRevision, int commitSequence)
		{
			var events = new[] { new EventMessage() }.ToList();
			return new Commit(streamId, streamRevision, Guid.NewGuid(), commitSequence, SystemTime.UtcNow, null, events);
		}

		protected Commit BuildCommitStub(Guid commitId, int streamRevision, int commitSequence)
		{
			var events = new[] { new EventMessage() }.ToList();
			return new Commit(streamId, streamRevision, commitId, commitSequence, SystemTime.UtcNow, null, events);
		}
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169