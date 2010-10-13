// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	public class when_saving_a_command_more_than_once : with_an_event_store
	{
		static readonly UncommittedEventStream uncomitted = new UncommittedEventStream
		{
			Id = Guid.NewGuid(),
			Events = new[] { "1", "2", "3" },
			CommandId = Guid.NewGuid()
		};

		static Exception exception;
		Because of = () =>
		{
			store.Write(uncomitted);
			uncomitted.ExpectedVersion = uncomitted.Events.Count;
			exception = Catch.Exception(() => store.Write(uncomitted));
		};

		It should_fail_by_throwing_a_DuplicateCommandException = () =>
			exception.ShouldBeOfType(typeof(DuplicateCommandException));

		It should_populate_the_exception_with_the_events_that_were_committed = () =>
			((DuplicateCommandException)exception).CommittedEvents.Count.ShouldEqual(uncomitted.Events.Count);
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming