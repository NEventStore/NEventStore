// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	public class when_attempting_to_write_a_nonexistant_expected_version : with_an_event_store
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
			uncomitted.ExpectedVersion = uncomitted.Events.Count + 15; // crazy optimistic concurrency value
			exception = Catch.Exception(() => store.Write(uncomitted));
		};

		It should_fail_by_throwing_a_StorageEngineException = () =>
			exception.ShouldBeOfType(typeof(StorageEngineException));
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming