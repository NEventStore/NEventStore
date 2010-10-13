// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	public class when_attempting_to_save_across_tenant_boundaries : with_an_event_store
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

			var anotherTenantStore = Build(Guid.NewGuid());

			uncomitted.ExpectedVersion = 1;

			exception = Catch.Exception(() => anotherTenantStore.Write(uncomitted));
		};

		It should_fail_by_throwing_a_StorageEngineException = () =>
			exception.ShouldBeOfType(typeof(StorageEngineException));
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming