// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	public class when_saving_an_event_stream_and_snapshot : with_an_event_store
	{
		static readonly UncommittedEventStream uncomitted = new UncommittedEventStream
		{
			Id = Guid.NewGuid(),
			Events = new[] { "1" },
			Snapshot = "snap"
		};

		static CommittedEventStream result;
		Because of = () =>
		{
			store.Write(uncomitted);
			result = store.Read(uncomitted.Id, uncomitted.Events.Count);
		};

		It should_persist_the_snapshot = () => result.Snapshot.ShouldEqual(uncomitted.Snapshot);
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming