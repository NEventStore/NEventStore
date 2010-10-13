// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	public class when_attempting_to_overwrite_a_committed_version : with_an_event_store
	{
		static readonly UncommittedEventStream uncomitted = new UncommittedEventStream
		{
			Id = Guid.NewGuid(),
			Events = new[] { "1", "2", "3" }
		};

		static Exception exception;
		Because of = () =>
		{
			store.Write(uncomitted);
			exception = Catch.Exception(() => store.Write(uncomitted));
		};

		It should_fail_by_throwing_a_ConcurrencyException = () =>
			exception.ShouldBeOfType(typeof(ConcurrencyException));

		It should_populate_the_exception_with_the_events_that_were_committed = () =>
			((ConcurrencyException)exception).CommittedEvents.Count.ShouldEqual(uncomitted.Events.Count);
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming