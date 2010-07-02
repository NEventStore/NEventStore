#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using Machine.Specifications;
	using Moq;
	using It=Machine.Specifications.It;

	[Subject("OptimisticEventStore")]
	public class when_reading_committed_events : with_the_event_stream
	{
		static readonly Guid id = Guid.NewGuid();
		static readonly CommittedEventStream fromStorage = new CommittedEventStream(id, 0, null, null);

		static CommittedEventStream actual;

		Establish context = () =>
			storageEngine.Setup(x => x.LoadById(id)).Returns(fromStorage);

		Because of = () =>
			actual = eventStore.Read(id);

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
			storageEngine.Setup(x => x.Save(Uncommitted, ExpectedVersion));

		Because of = () =>
			eventStore.Write(Uncommitted);

		It should_pass_the_stream_to_the_underlying_storage_engine_as_version_zero = () =>
			storageEngine.VerifyAll();
	}

	[Subject("OptimisticEventStore")]
	public class when_appending_to_an_existing_event_stream : with_the_event_stream
	{
		const long ExpectedVersion = 17;
		static readonly CommittedEventStream Committed = new CommittedEventStream(Guid.NewGuid(), ExpectedVersion, null, null);
		static readonly UncommittedEventStream Uncommitted = new UncommittedEventStream
		{
			Id = Committed.Id,
			Events = new object[1]
		};

		Establish context = () =>
		{
			storageEngine.Setup(x => x.LoadById(Committed.Id)).Returns(Committed);
			storageEngine.Setup(x => x.Save(Uncommitted, ExpectedVersion));
		};

		Because of = () =>
		{
			eventStore.Read(Committed.Id);
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
			storageEngine.Setup(x => x.Save(Uncommitted, 0));

		Because of = () =>
		    eventStore.Write(Uncommitted);

		It should_not_invoke_the_underlying_storage_engine = () =>
			storageEngine.Verify(x => x.Save(Uncommitted, 0), Times.Never());
	}

	[Subject("OptimisticEventStore")]
	public class when_writing_to_an_aggregate_that_has_been_updated_by_another_session : with_the_event_stream
	{
		const long ExpectedVersion = 42;
		static readonly CommittedEventStream Committed = new CommittedEventStream(Guid.NewGuid(), ExpectedVersion, null, null);
		static readonly ICollection EventsOnConcurrencyException = new object[1];
		static readonly UncommittedEventStream Uncommitted = new UncommittedEventStream
		{
			Id = Committed.Id,
			Events = new object[1]
		};

		static ConcurrencyException expectedException;

		Establish context = () =>
		{
			storageEngine.Setup(x => x.LoadById(Committed.Id)).Returns(Committed);
			storageEngine.Setup(x => x.Save(Uncommitted, ExpectedVersion)).Throws(new DuplicateKeyException());
			storageEngine.Setup(x => x.LoadStartingAfter(Committed.Id, ExpectedVersion)).Returns(EventsOnConcurrencyException);
		};

		Because of = () =>
		{
			eventStore.Read(Committed.Id);

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
		Establish context;
		Because of;
		It should_query_the_underlying_storage_for_the_committed_events_resulting_from_the_command;
		It should_throw_a_duplicate_command_exception;
		It should_populate_the_exception_with_the_committed_events;
	}

	public abstract class with_the_event_stream
	{
		protected static Mock<IStorageEngine> storageEngine;
		protected static OptimisticEventStore eventStore;

		Establish context = () =>
		{
			storageEngine = new Mock<IStorageEngine>();
			eventStore = new OptimisticEventStore(storageEngine.Object);
		};
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming