// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	[Subject("Cross-tenant Security:")]
	public class when_attempting_to_overwrite_a_new_stream_across_tenant_boundaries : with_an_event_store
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
			exception = Catch.Exception(() => anotherTenantStore.Write(uncomitted));
		};

		It should_fail_by_throwing_a_CrossTenantAccessException = () =>
			exception.ShouldBeOfType(typeof(CrossTenantAccessException));
	}

	[Subject("Cross-tenant Security:")]
	public class when_attempting_to_update_an_existing_stream_across_tenant_boundaries : with_an_event_store
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
			uncomitted.CommittedVersion = 1; // update the stream, don't create a new one
			exception = Catch.Exception(() => anotherTenantStore.Write(uncomitted));
		};

		It should_fail_by_throwing_a_CrossTenantAccessException = () =>
			exception.ShouldBeOfType(typeof(CrossTenantAccessException));
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming