#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using System.Collections;
	using Machine.Specifications;
	using Moq;
	using It=Machine.Specifications.It;

	[Subject("OptimisticEventStore")]
	public class when_reading_committed_events : with_the_event_stream
	{
		const int StartingVerion = 17;
		static readonly Guid id = Guid.NewGuid();
		static readonly CommittedEventStream fromStorage = new CommittedEventStream(id, 0, null, null, null);

		static CommittedEventStream actual;

		Establish context = () =>
			storageEngine.Setup(x => x.LoadById(id, StartingVerion)).Returns(fromStorage);

		Because of = () =>
			actual = eventStore.Read(id, StartingVerion);

		It should_call_through_to_the_underlying_storage_engine_using_the_id_provided = () =>
			storageEngine.VerifyAll();

		It should_return_the_committed_event_stream_from_the_storage_engine = () =>
			actual.ShouldEqual(fromStorage);
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_a_new_event_stream_for_a_new_aggregate : with_the_event_stream
	{
		const long ExpectedVersion = 0;
		static readonly UncommittedEventStream Uncommitted = new UncommittedEventStream
		{
			Events = new object[1]
		};

		Establish context = () =>
			storageEngine.Setup(x => x.Save(Uncommitted));

		Because of = () =>
			eventStore.Write(Uncommitted);

		It should_pass_the_stream_to_the_underlying_storage_engine_as_version_zero = () =>
			storageEngine.VerifyAll();
	}

	[Subject("OptimisticEventStore")]
	public class when_appending_to_an_existing_event_stream : with_the_event_stream
	{
		const long StartingVersion = 15;
		const long ExpectedVersion = 17;
		static readonly CommittedEventStream Committed = new CommittedEventStream(
			Guid.NewGuid(), ExpectedVersion, null, null, null);
		static readonly UncommittedEventStream Uncommitted = new UncommittedEventStream
		{
			Id = Committed.Id,
			Events = new object[1]
		};

		Establish context = () =>
		{
			storageEngine.Setup(x => x.LoadById(Committed.Id, StartingVersion)).Returns(Committed);
			storageEngine.Setup(x => x.Save(Uncommitted));
		};

		Because of = () =>
		{
			eventStore.Read(Committed.Id, StartingVersion);
			eventStore.Write(Uncommitted);
		};

		It should_pass_the_stream_and_loaded_version_to_the_underlying_storage_engine = () =>
			storageEngine.VerifyAll();
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_an_empty_stream : with_the_event_stream
	{
		static readonly UncommittedEventStream Uncommitted = new UncommittedEventStream();

		Establish context = () =>
			storageEngine.Setup(x => x.Save(Uncommitted));

		Because of = () =>
		{
			try
			{
				eventStore.Write(Uncommitted);
			}
			catch (ArgumentException) { }
		};

		It should_not_invoke_the_underlying_storage_engine = () =>
			storageEngine.Verify(x => x.Save(Uncommitted), Times.Never());
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_to_an_aggregate_that_has_been_updated_by_another_session : with_the_event_stream
	{
		const long StartingVersion = 17;
		const long ExpectedVersion = 42;
		static readonly CommittedEventStream Committed = new CommittedEventStream(
			Guid.NewGuid(), ExpectedVersion, null, null, null);
		static readonly ICollection EventsOnConcurrencyException = new object[1];
		static readonly UncommittedEventStream Uncommitted = new UncommittedEventStream
		{
			Id = Committed.Id,
			Events = new object[1],
			ExpectedVersion = ExpectedVersion
		};

		static ConcurrencyException expectedException;

		Establish context = () =>
		{
			storageEngine.Setup(x => x.LoadById(Committed.Id, StartingVersion)).Returns(Committed);
			storageEngine.Setup(x => x.Save(Uncommitted)).Throws(new DuplicateKeyException());
			storageEngine.Setup(x => x.LoadStartingAfter(Committed.Id, ExpectedVersion)).Returns(EventsOnConcurrencyException);
		};

		Because of = () =>
		{
			eventStore.Read(Committed.Id, StartingVersion);

			try
			{
				eventStore.Write(Uncommitted);
			}
			catch (ConcurrencyException e)
			{
				expectedException = e;
			}
		};

		It should_query_the_underlying_storage_using_the_id_and_version_at_which_the_aggregate_was_originally_loaded = () =>
			storageEngine.Verify(x => x.LoadStartingAfter(Committed.Id, ExpectedVersion));

		It should_throw_a_concurrency_exception = () =>
			expectedException.ShouldNotBeNull();

		It should_populate_the_exception_thrown_with_committed_events_from_the_other_session = () =>
			expectedException.CommittedEvents.ShouldEqual(EventsOnConcurrencyException);
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_events_for_a_previously_handled_command : with_the_event_stream
	{
		static readonly ICollection DuplicateEvents = new object[0];
		static readonly UncommittedEventStream stream = new UncommittedEventStream
		{
			Id = Guid.NewGuid(),
			Command = Guid.NewGuid(),
			Events = new object[1]
		};

		static DuplicateCommandException expected;

		Establish context = () =>
		{
			storageEngine.Setup(x => x.Save(stream)).Throws(new DuplicateKeyException());
			storageEngine.Setup(x => x.LoadStartingAfter(stream.Id, 0)).Returns(new object[0]);
			storageEngine.Setup(x => x.LoadByCommandId(stream.CommandId)).Returns(DuplicateEvents);
		};

		Because of = () =>
		{
			try
			{
				eventStore.Write(stream);
			}
			catch (DuplicateCommandException e)
			{
				expected = e;
			}
		};

		It should_query_the_underlying_storage_for_the_committed_events_resulting_from_the_command = () =>
			storageEngine.Verify(x => x.LoadByCommandId(stream.CommandId));

		It should_throw_a_duplicate_command_exception = () =>
			expected.ShouldNotBeNull();

		It should_populate_the_exception_with_the_committed_events = () =>
			expected.CommittedEvents.ShouldEqual(DuplicateEvents);
	}

	public abstract class with_the_event_stream
	{
		protected static Mock<IAdaptStorage> storageEngine;
		protected static OptimisticEventStore eventStore;

		Establish context = () =>
		{
			storageEngine = new Mock<IAdaptStorage>();
			eventStore = new OptimisticEventStore(storageEngine.Object);
		};
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming