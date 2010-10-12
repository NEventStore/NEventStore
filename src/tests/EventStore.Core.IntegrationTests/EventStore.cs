#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	[Subject("OptimisticEventStore")]
	public class when_saving_to_the_event_store : from_an_event_stream
	{
		private Establish context = () =>
			store = EventStoreFactory.Build("SQLite");

		private Because of = () =>
			store.Write(uncomitted);

		private It should_persist_the_snapshot = () =>
		    store.Read(StreamId, 0).Snapshot.ShouldEqual(Snapshot);
	}

	public abstract class from_an_event_stream
	{
		protected static Guid StreamId = Guid.NewGuid();
		protected static Guid CommandId = Guid.NewGuid();
		protected static object Command = "command message";
		protected static object Snapshot = "snapshot";
		protected static string[] Events = new[] { "1st event", "2nd event" };

		protected static IStoreEvents store;

		protected static UncommittedEventStream uncomitted = new UncommittedEventStream
		{
			Id = StreamId,
			Command = Command,
			CommandId = CommandId,
			Events = Events,
			ExpectedVersion = 0,
			Snapshot = Snapshot,
			Type = typeof(string)
		};

		private Establish context = () =>
			AppDomain.CurrentDomain.SetData("DataDirectory", Environment.CurrentDirectory);
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming