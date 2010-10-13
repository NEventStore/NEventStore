// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	public class when_attempting_to_write_beyond_the_end_of_a_stream : with_an_event_store
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
			uncomitted.CommittedVersion = uncomitted.Events.Count + 15; // crazy optimistic concurrency value
			exception = Catch.Exception(() => store.Write(uncomitted));
		};

		It should_fail_by_throwing_a_CrossTenantAccessException = () =>
			exception.ShouldBeOfType(typeof(CrossTenantAccessException));
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming