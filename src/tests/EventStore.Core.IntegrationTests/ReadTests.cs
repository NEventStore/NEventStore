// ReSharper disable InconsistentNaming
#pragma warning disable 169

namespace EventStore.Core.IntegrationTests
{
	using System;
	using Machine.Specifications;

	[Subject("Read")]
	public class when_loading_a_snapshotted_stream_from_zero : with_an_event_store
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

			uncomitted.CommittedVersion += uncomitted.Events.Count;
			store.Write(uncomitted);

			uncomitted.Snapshot = "take snapshot";
			uncomitted.CommittedVersion += uncomitted.Events.Count;
			store.Write(uncomitted);

			result = store.Read(uncomitted.Id, 0);
		};

		It should_have_the_correct_committed_version = () =>
			result.Version.ShouldEqual(uncomitted.CommittedVersion + uncomitted.Events.Count);

		It should_not_contain_a_snapshot = () =>
			result.Snapshot.ShouldBeNull();

		It should_contain_all_of_the_events = () =>
			result.Events.Count.ShouldEqual((int)result.Version);
	}

	[Subject("Read")]
	public class when_loading_a_stream_from_a_point_in_time : with_an_event_store
	{
		const int MaxLoadAtVersion = 8; // find the first snapshot up to (but not beyond) this point
		static readonly UncommittedEventStream uncomitted = new UncommittedEventStream
		{
			Id = Guid.NewGuid(),
			Events = new[] { "1", "2", "3" }
		};

		static CommittedEventStream result;
		Because of = () =>
		{
			uncomitted.Snapshot = "first";
			uncomitted.CommittedVersion = 0;
			store.Write(uncomitted); // version is now 3

			uncomitted.Snapshot = "second";
			uncomitted.CommittedVersion = 3;
			store.Write(uncomitted); // version is now 6

			uncomitted.Snapshot = "third";
			uncomitted.CommittedVersion = 6;
			store.Write(uncomitted); // version is now 9

			uncomitted.Snapshot = "fourth";
			uncomitted.CommittedVersion = 9;
			store.Write(uncomitted); // version is now 12

			uncomitted.Snapshot = null; // no snapshot
			uncomitted.CommittedVersion = 12;
			store.Write(uncomitted); // version is now 15

			result = store.Read(uncomitted.Id, MaxLoadAtVersion);
		};

		It should_have_the_correct_committed_version = () =>
			result.Version.ShouldEqual(15);

		It should_load_the_correct_snapshot = () =>
			result.Snapshot.ShouldEqual("second");

		It should_contain_all_of_the_events_since_the_snapshot = () =>
			result.Events.Count.ShouldEqual(9); // 9 events have occurred since the second snapshot
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming