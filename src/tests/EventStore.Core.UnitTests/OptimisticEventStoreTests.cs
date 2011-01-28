#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using System.Linq;
	using Dispatcher;
	using Machine.Specifications;
	using Moq;
	using Persistence;
	using It = Machine.Specifications.It;

	[Subject("OptimisticEventStore")]
	public class when_opening_a_stream : using_persistence
	{
		const int MinRevision = 17;
		const int MaxRevision = 42;
		static readonly Commit[] Committed = new Commit[] { };
		static IEventStream stream;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, MinRevision, MaxRevision)).Returns(Committed);

		Because of = () =>
			stream = store.OpenStream(streamId, MinRevision, MaxRevision);

		It should_invoke_the_underlying_infrastructure_with_the_values_provided = () =>
			persistence.Verify(x => x.GetFrom(streamId, MinRevision, MaxRevision), Times.Once());

		It should_return_an_event_stream_containing_the_correct_stream_identifer = () =>
			stream.StreamId.ShouldEqual(streamId);

		Cleanup cleanup = () =>
			stream.Dispose();
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_from_reversion_zero : using_persistence
	{
		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0, int.MaxValue)).Returns(new Commit[] { });

		Because of = () =>
			store.GetFrom(streamId, 0, int.MaxValue).ToList();

		It should_pass_a_revision_range_to_the_persistence_infrastructure = () =>
			persistence.Verify(x => x.GetFrom(streamId, 0, int.MaxValue), Times.Once());
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_a_null_attempt_back_to_the_stream : using_persistence
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Commit(null));

		It should_throw_an_ArgumentNullException = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_an_unidentified_attempt_back_to_the_stream : using_persistence
	{
		static readonly Guid emptyIdentifier = Guid.Empty;
		static readonly Commit unidentified = BuildCommitStub(emptyIdentifier);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Commit(unidentified));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_the_number_of_commits_is_greater_than_the_number_of_revisions : using_persistence
	{
		const int StreamRevision = 1;
		const int CommitSequence = 2; // should never be greater than StreamRevision.
		static readonly Commit corrupt = BuildCommitStub(StreamRevision, CommitSequence);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Commit(corrupt));

		It should_throw_a_StorageException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_nonpositive_commit_sequence_back_to_the_stream : using_persistence
	{
		const int StreamRevision = 1;
		const int InvalidCommitSequence = 0;
		static readonly Commit invalidCommitSequence = BuildCommitStub(StreamRevision, InvalidCommitSequence);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Commit(invalidCommitSequence));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_non_positive_stream_revision_back_to_the_stream : using_persistence
	{
		const int InvalidStreamRevision = 0;
		const int CommitSequence = 1;
		static readonly Commit invalidStreamRevision = BuildCommitStub(InvalidStreamRevision, CommitSequence);
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => store.Commit(invalidStreamRevision));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_sequence_beyond_the_known_end_of_a_stream : using_persistence
	{
		const int HeadStreamRevision = 5;
		const int HeadCommitSequence = 1;
		const int ExpectedNextCommitSequence = HeadCommitSequence + 1;
		const int BeyondEndOfStreamCommitSequence = ExpectedNextCommitSequence + 1;
		static readonly Commit beyondEndOfStream = BuildCommitStub(HeadStreamRevision + 1, BeyondEndOfStreamCommitSequence);
		static readonly Commit[] alreadyCommitted = new[]
		{
			BuildCommitStub(HeadStreamRevision, HeadCommitSequence)
		};
		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0, int.MaxValue)).Returns(alreadyCommitted);

		Because of = () =>
		{
			store.GetFrom(streamId, 0, int.MaxValue).ToList();
			thrown = Catch.Exception(() => store.Commit(beyondEndOfStream));
		};

		It should_throw_a_PersistenceException = () =>
			thrown.ShouldBeOfType<StorageException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_revision_beyond_the_known_end_of_a_stream : using_persistence
	{
		const int HeadCommitSequence = 1;
		const int HeadStreamRevision = 1;
		const int NumberOfEventsBeingCommitted = 1;
		const int ExpectedNextStreamRevision = HeadStreamRevision + 1 + NumberOfEventsBeingCommitted;
		const int BeyondEndOfStreamRevision = ExpectedNextStreamRevision + 1;

		static readonly Commit[] alreadyCommitted = new[]
		{
			BuildCommitStub(HeadStreamRevision, HeadCommitSequence)
		};
		static readonly Commit beyondEndOfStream = BuildCommitStub(
			BeyondEndOfStreamRevision, HeadCommitSequence + 1);
		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0, int.MaxValue)).Returns(alreadyCommitted);

		Because of = () =>
		{
			store.GetFrom(streamId, 0, int.MaxValue).ToList();
			thrown = Catch.Exception(() => store.Commit(beyondEndOfStream));
		};

		It should_throw_a_PersistenceException = () =>
			thrown.ShouldBeOfType<StorageException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_an_empty_attempt_to_a_stream : using_persistence
	{
		static readonly Commit attemptWithNoEvents = BuildCommitStub(Guid.NewGuid());

		Establish context = () =>
			persistence.Setup(x => x.Commit(attemptWithNoEvents));

		Because of = () =>
			store.Commit(attemptWithNoEvents);

		It should_drop_the_commit_provided = () =>
			persistence.Verify(x => x.Commit(attemptWithNoEvents), Times.AtMost(0));
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_valid_and_populated_attempt_to_a_stream : using_persistence
	{
		static readonly Commit populatedAttempt = BuildCommitStub(1, 1);

		Establish context = () =>
		{
			persistence.Setup(x => x.Commit(populatedAttempt));
			dispatcher.Setup(x => x.Dispatch(populatedAttempt));
		};

		Because of = () =>
			store.Commit(populatedAttempt);

		It should_provide_the_commit_attempt_to_the_configured_persistence_mechanism = () =>
			persistence.Verify(x => x.Commit(populatedAttempt), Times.Once());

		It should_provide_the_commit_to_the_dispatcher = () =>
			dispatcher.Verify(x => x.Dispatch(populatedAttempt), Times.Once());
	}

	/// <summary>
	/// This behavior is primarily to support a NoSQL storage solution where CommitId is not being used as the "primary key"
	/// in a NoSQL environment, we'll most likely use StreamId + CommitSequence, which also enables optimistic concurrency.
	/// </summary>
	[Subject("OptimisticEventStore")]
	public class when_committing_with_an_identifier_that_was_previously_read : using_persistence
	{
		const int MaxRevision = 2;
		static readonly Guid AlreadyCommittedId = Guid.NewGuid();
		static readonly Commit[] Committed = new[]
	    {
			BuildCommitStub(AlreadyCommittedId, 1, 1),
			BuildCommitStub(Guid.NewGuid(), 1, 1)
	    };
		static readonly Commit DuplicateCommitAttempt = BuildCommitStub(
			AlreadyCommittedId, Committed.Last().StreamRevision + 1, Committed.Last().CommitSequence + 1);
		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, 0, MaxRevision)).Returns(Committed);

		Because of = () =>
		{
			store.GetFrom(streamId, 0, MaxRevision).ToList();
			thrown = Catch.Exception(() => store.Commit(DuplicateCommitAttempt));
		};

		It should_throw_a_DuplicateCommitException = () =>
			thrown.ShouldBeOfType<DuplicateCommitException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_the_same_commit_identifier_more_than_once : using_persistence
	{
		static readonly Guid DuplicateCommitId = Guid.NewGuid();
		static readonly Commit SuccessfulCommit = BuildCommitStub(DuplicateCommitId, 1, 1);
		static readonly Commit DuplicateCommit = BuildCommitStub(DuplicateCommitId, 2, 2);
		static Exception thrown;

		Establish context = () =>
			store.Commit(SuccessfulCommit);

		Because of = () =>
			thrown = Catch.Exception(() => store.Commit(DuplicateCommit));

		It throw_a_DuplicateCommitException = () =>
			thrown.ShouldBeOfType<DuplicateCommitException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_sequence_less_or_equal_to_the_most_recent_sequence_for_the_stream : using_persistence
	{
		const int HeadStreamRevision = 42;
		const int HeadCommitSequence = 42;
		const int DupliateCommitSequence = HeadCommitSequence;
		static readonly Commit[] Committed = new[] { BuildCommitStub(HeadStreamRevision, HeadCommitSequence) };
		private static readonly Commit Attempt = BuildCommitStub(HeadStreamRevision + 1, DupliateCommitSequence);

		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, HeadStreamRevision, int .MaxValue)).Returns(Committed);

		Because of = () =>
		{
			store.GetFrom(streamId, HeadStreamRevision, int.MaxValue).ToList();
			thrown = Catch.Exception(() => store.Commit(Attempt));
		};

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_revision_less_or_equal_to_than_the_most_recent_revision_read_for_the_stream : using_persistence
	{
		const int HeadStreamRevision = 3;
		const int HeadCommitSequence = 2;
		const int DuplicateStreamRevision = HeadStreamRevision;
		static readonly Commit[] Committed = new[] { BuildCommitStub(HeadStreamRevision, HeadCommitSequence) };
		static readonly Commit FailedAttempt = BuildCommitStub(DuplicateStreamRevision, HeadCommitSequence + 1);

		static Exception thrown;

		Establish context = () =>
			persistence.Setup(x => x.GetFrom(streamId, HeadStreamRevision, int.MaxValue)).Returns(Committed);

		Because of = () =>
		{
			store.GetFrom(streamId, HeadStreamRevision, int.MaxValue).ToList();
			thrown = Catch.Exception(() => store.Commit(FailedAttempt));
		};

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_commit_sequence_less_than_or_equal_to_the_most_recent_commit_for_the_stream : using_persistence
	{
		const int DuplicateCommitSequence = 1;

		static readonly Commit SuccessfulAttempt = BuildCommitStub(1, DuplicateCommitSequence);
		static readonly Commit FailedAttempt = BuildCommitStub(2, DuplicateCommitSequence);
		static Exception thrown;

		Establish context = () =>
			store.Commit(SuccessfulAttempt);

		Because of = () =>
			thrown = Catch.Exception(() => store.Commit(FailedAttempt));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_committing_with_a_stream_revision_less_than_or_equal_to_the_most_recent_commit_for_the_stream : using_persistence
	{
		const int DuplicateStreamRevision = 2;

		static readonly Commit SuccessfulAttempt = BuildCommitStub(DuplicateStreamRevision, 1);
		static readonly Commit FailedAttempt = BuildCommitStub(DuplicateStreamRevision, 2);
		static Exception thrown;

		Establish context = () =>
			store.Commit(SuccessfulAttempt);

		Because of = () =>
			thrown = Catch.Exception(() => store.Commit(FailedAttempt));

		It should_throw_a_ConcurrencyException = () =>
			thrown.ShouldBeOfType<ConcurrencyException>();
	}

	public abstract class using_persistence
	{
		protected static Guid streamId = Guid.NewGuid();
		protected static Mock<IPersistStreams> persistence;
		protected static Mock<IDispatchCommits> dispatcher;
		protected static OptimisticEventStore store;

		Establish context = () =>
		{
			persistence = new Mock<IPersistStreams>();
			dispatcher = new Mock<IDispatchCommits>();
			store = new OptimisticEventStore(persistence.Object, dispatcher.Object);
		};

		Cleanup everything = () =>
			streamId = Guid.NewGuid();

		protected static Commit BuildCommitStub(Guid commitId)
		{
			return new Commit(streamId, 1, commitId, 1, null, null, null);
		}
		protected static Commit BuildCommitStub(int streamRevision, int commitSequence)
		{
			var events = new[] { new EventMessage() } .ToList();
			return new Commit(streamId, streamRevision, Guid.NewGuid(), commitSequence, null, events, null);
		}
		protected static Commit BuildCommitStub(Guid commitId, int streamRevision, int commitSequence)
		{
			var events = new[] { new EventMessage() } .ToList();
			return new Commit(streamId, streamRevision, commitId, commitSequence, null, events, null);
		}
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169