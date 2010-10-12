// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	public class when_savings_an_event_stream : with_an_event_store
	{
		static readonly Guid StreamId = Guid.NewGuid();
		static readonly Guid CommandId = Guid.NewGuid();
		static readonly object Command = "command message";
		static readonly object Snapshot = "snapshot";
		static readonly string[] Events = new[] { "1st event", "2nd event" };
		static readonly UncommittedEventStream uncomitted = new UncommittedEventStream
		{
			Id = StreamId,
			Command = Command,
			CommandId = CommandId,
			Events = Events,
			ExpectedVersion = 0,
			Snapshot = Snapshot,
			Type = typeof(string)
		};

		static CommittedEventStream result;
		Because of = () =>
		{
			store.Write(uncomitted);
			result = store.Read(uncomitted.Id, 0);
		};

		It should_increment_the_committed_version_number = () => result.Version.ShouldEqual(2);
		It should_persist_the_correct_number_of_events = () => result.Events.Count.ShouldEqual(2);
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming