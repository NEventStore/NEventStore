// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using System.Linq;
	using Machine.Specifications;

	public class when_savings_an_event_stream : with_an_event_store
	{
		static readonly UncommittedEventStream uncomitted = new UncommittedEventStream
		{
			Id = Guid.NewGuid(),
			Events = new[] { "1", "2", "3" }
		};

		static CommittedEventStream result;
		Because of = () =>
		{
			store.Write(uncomitted);
			result = store.Read(uncomitted.Id, 0);
		};

		It should_save_the_correct_stream_identifier = () => result.Id.ShouldEqual(uncomitted.Id);
		It should_increment_the_stream_version_number = () => result.Version.ShouldEqual(uncomitted.Events.Count);
		It should_persist_the_all_events = () => result.Events.Count.ShouldEqual(uncomitted.Events.Count);
		It should_persist_the_contents_of_each_event_correctly = () =>
		{
			var committedEvents = result.Events.Cast<object>().ToArray();
			for (var i = 0; i < committedEvents.Length; i++)
				committedEvents[i].ShouldEqual(uncomitted.Events.Cast<object>().ToArray()[i]);
		};
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming