#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using Persistence;
	using It = Machine.Specifications.It;

	[Subject("OptimisticEventStore")]
	public class when_reading_a_stream_up_to_a_maximum_revision : using_persistence
	{
		const long MaxRevision = 1234;
		static readonly Commit[] Commits = new[]
		{
			new Commit(StreamId, Guid.NewGuid(), 1, 1, null, null, "ignore this snapshot")
			{
				Events = { new EventMessage { Body = 1 } }
			}, 
			new Commit(StreamId, Guid.NewGuid(), 3, 2, null, null, "use this snapshot")
			{
				Events =
				{
					new EventMessage { Body = 2 },
					new EventMessage { Body = 3 }
				}
			}, 
			new Commit(StreamId, Guid.NewGuid(), 5, 3, null, null, null)
			{
				Events =
				{
					new EventMessage { Body = 4 },
					new EventMessage { Body = 5 }
				}
			}
		};

		static CommittedEventStream actual;

		Establish context = () =>
			Persistence.Setup(x => x.GetUntil(StreamId, MaxRevision)).Returns(Commits);

		Because of = () =>
			actual = Store.ReadUntil(StreamId, MaxRevision);

		It should_query_the_configured_persistence_mechanism = () =>
			Persistence.VerifyAll();

		It should_ignore_events_prior_to_the_most_recent_snapshot_retreived = () =>
			actual.StreamRevision.ShouldEqual(Commits.MostRecentRevision());

		It should_populate_the_stream_with_the_most_recent_snapshot = () =>
			actual.Snapshot.ShouldEqual(Commits.MostRecentSnapshot());

		It should_ignore_an_earlier_snapshot = () =>
			actual.Snapshot.ShouldNotEqual(Commits[0].Snapshot);

		It should_only_contain_the_events_from_commits_after_the_most_recent_snapshot = () =>
			actual.Events.Count.ShouldEqual(Commits.Last().Events.Count);

		It should_order_the_events_from_oldest_to_newest = () =>
			actual.Events.Cast<object>().Last().ShouldEqual(Commits.NewestEvent());
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_a_stream_from_a_minimum_revision : using_persistence
	{
		const long MinRevision = 42;
		static readonly Commit[] Commits = new[]
		{
			new Commit(StreamId, Guid.NewGuid(), 1, 1, null, null, "ignore this snapshot")
			{
				Events = { new EventMessage { Body = 1 } }
			}, 
			new Commit(StreamId, Guid.NewGuid(), 3, 2, null, null, "ignore this snapshot too")
			{
				Events =
				{
					new EventMessage { Body = 2 },
					new EventMessage { Body = 3 }
				}
			}, 
			new Commit(StreamId, Guid.NewGuid(), 6, 3, null, null, null)
			{
				Events =
				{
					new EventMessage { Body = 4 },
					new EventMessage { Body = 5 },
					new EventMessage { Body = 6 }
				}
			}
		};

		static CommittedEventStream actual;

		Establish context = () =>
			Persistence.Setup(x => x.GetFrom(StreamId, MinRevision)).Returns(Commits);

		Because of = () =>
			actual = Store.ReadFrom(StreamId, MinRevision);

		It should_query_the_configured_persistence_mechanism = () =>
			Persistence.VerifyAll();

		It should_completely_ignore_snapshots_during_stream_reconstruction = () =>
			actual.Events.Count.ShouldEqual(Commits.CountEvents());

		It should_put_the_oldest_event_as_the_first_event = () =>
			actual.Events.Cast<object>().First().ShouldEqual(Commits.OldestEvent());

		It should_put_the_newest_event_as_the_last_event = () =>
			actual.Events.Cast<object>().Last().ShouldEqual(Commits.NewestEvent());
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_null_commit_attempt_back_to_the_stream : using_persistence
	{
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => Store.Write(null));

		It should_throw_an_ArgumentNullException = () =>
			thrown.ShouldBeOfType<ArgumentNullException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_unidentified_commit_attempt_back_to_the_stream : using_persistence
	{
		static readonly CommitAttempt unidentified = new CommitAttempt();
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => Store.Write(unidentified));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_commit_attempt_with_a_negative_commit_sequence_back_to_the_stream : using_persistence
	{
		static readonly CommitAttempt negativeCommitSequence = new CommitAttempt
		{
			CommitId = Guid.NewGuid(),
			CommitSequence = -1
		};
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => Store.Write(negativeCommitSequence));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_commit_attempt_with_a_negative_stream_revision_back_to_the_stream : using_persistence
	{
		static readonly CommitAttempt negativeStreamRevision = new CommitAttempt
		{
			CommitId = Guid.NewGuid(),
			CommitSequence = 1,
			StreamRevision = -1
		};
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => Store.Write(negativeStreamRevision));

		It should_throw_an_ArgumentException = () =>
			thrown.ShouldBeOfType<ArgumentException>();

		It should_throw_an_ArgumentNullException;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_empty_commit_attempt_back_to_the_stream : using_persistence
	{
		static readonly CommitAttempt attemptWithNoEvents = new CommitAttempt
		{
			CommitId = Guid.NewGuid(),
			CommitSequence = 1,
			StreamRevision = 1
		};

		Establish context = () =>
			Persistence.Setup(x => x.Persist(attemptWithNoEvents));

		Because of = () =>
			Store.Write(attemptWithNoEvents);

		It should_drop_the_commit_provided = () =>
			Persistence.Verify(x => x.Persist(attemptWithNoEvents), Times.AtMost(0));
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_attempt_back_to_the_stream : using_persistence
	{
		static readonly CommitAttempt populatedAttempt = new CommitAttempt
		{
			CommitId = Guid.NewGuid(),
			CommitSequence = 1,
			StreamRevision = 1,
			Events = { new EventMessage() }
		};

		Establish context = () =>
			Persistence.Setup(x => x.Persist(populatedAttempt));

		Because of = () =>
			Store.Write(populatedAttempt);

		It should_provide_the_valid_commit_attempt_to_the_configured_persistence_mechanism = () =>
			Persistence.Verify(x => x.Persist(populatedAttempt), Times.Exactly(1));
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_attempt_with_an_identifier_that_has_already_been_read : using_persistence
	{
		It should_throw_a_DuplicateCommitException;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_attempt_with_a_commit_sequence_less_than_or_equal_to_the_most_recent_commit_read : using_persistence
	{
		It should_throw_a_ConcurrencyException;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_attempt_with_a_stream_revision_less_than_or_equal_to_the_most_stream_revision_read : using_persistence
	{
		It should_throw_a_ConcurrencyException;
	}

	public abstract class using_persistence
	{
		protected static readonly Guid StreamId = Guid.NewGuid();
		protected static readonly Mock<IPersistStreams> Persistence = new Mock<IPersistStreams>();
		protected static readonly OptimisticEventStore Store = new OptimisticEventStore(Persistence.Object);
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169