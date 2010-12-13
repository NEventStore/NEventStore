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
			new Commit(StreamId, Guid.NewGuid(), 1, null, null, "ignore this snapshot")
			{
				Events = { new EventMessage { StreamRevision = 1, Body = "ignore this event" } }
			}, 
			new Commit(StreamId, Guid.NewGuid(), 2, null, null, "use this snapshot")
			{
				Events = { new EventMessage { StreamRevision = 2, Body = "ignore this event too" } }
			}, 
			new Commit(StreamId, Guid.NewGuid(), 3, null, null, null)
			{
				Events = { new EventMessage { StreamRevision = 3, Body = "use this event" } }
			}, 
		};

		static CommittedEventStream actual;

		Establish context = () =>
			Persistence.Setup(x => x.GetUntil(StreamId, MaxRevision)).Returns(Commits);

		Because of = () =>
			actual = Store.ReadUntil(StreamId, MaxRevision);

		It should_query_the_configured_persistence_engine = () =>
			Persistence.VerifyAll();

		It should_ignore_events_prior_to_the_most_recent_snapshot_retreived = () =>
			actual.StreamRevision.ShouldEqual(Commits.Last().Events.Last().StreamRevision);

		It should_populate_the_stream_with_the_most_recent_snapshot = () =>
			actual.Snapshot.ShouldEqual(Commits.Where(x => x.Snapshot != null).Last().Snapshot);

		It should_ignore_an_earlier_snapshot = () =>
			actual.Snapshot.ShouldNotEqual(Commits[0].Snapshot);
	}

	[Subject("OptimisticEventStore")]
	public class when_reading_a_stream_from_a_minimum_revision
	{
		It should_query_the_configured_persistence_engine;
		It should_complete_ignore_snapshots_during_stream_reconstruction;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_invalid_commit_back_to_the_stream
	{
		It should_drop_the_commit_provided;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_back_to_the_stream
	{
		It should_write_the_commit_to_the_configured_persistence_engine;
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_commit_with_an_identifier_that_has_already_been_read
	{
		It should_throw_a_DuplicateCommitException;
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