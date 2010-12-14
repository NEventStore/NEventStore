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
	public class when_reading_a_stream_until_a_maximum_revision : from_persistence
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

		It should_contain_the_most_recent_events_after_the_snapshot = () =>
			actual.Events.Count.ShouldEqual(Commits.Last().Events.Count);

		It should_order_the_events_from_oldest_to_newest = () =>
			actual.Events.Cast<object>().Last().ShouldEqual(Commits.NewestEvent());
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_a_stream_from_a_minimum_revision : from_persistence
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
	public class when_writing_an_invalid_commit_back_to_the_stream : from_persistence
	{
		It should_drop_the_commit_provided;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_back_to_the_stream : from_persistence
	{
		It should_write_the_commit_to_the_configured_persistence_mechanism;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_with_an_identifier_that_has_already_been_read : from_persistence
	{
		It should_throw_a_DuplicateCommitException;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_with_a_commit_sequence_less_than_or_equal_to_the_most_recent_commit_read : from_persistence
	{
		It should_throw_a_ConcurrencyException;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_with_a_stream_revision_less_than_or_equal_to_the_most_stream_revision_read : from_persistence
	{
		It should_throw_a_ConcurrencyException;
	}

	public abstract class from_persistence
	{
		protected static readonly Guid StreamId = Guid.NewGuid();
		protected static readonly Mock<IPersistStreams> Persistence = new Mock<IPersistStreams>();
		protected static readonly OptimisticEventStore Store = new OptimisticEventStore(Persistence.Object);
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169