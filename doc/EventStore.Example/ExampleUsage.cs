namespace EventStore.Example
{
	using System;

	internal static class ExampleUsage
	{
		private static readonly Guid StreamId = Guid.NewGuid(); // this is generally your aggregate identifer
		private static IStoreEvents eventStore;

		public static void Show(IStoreEvents store)
		{
			eventStore = store;
			
			CreateStream();
			AppendToStream();
			TakeSnapshot();
			LoadFromSnapshotForwardAndAppend();
		}

		private static void CreateStream()
		{
			using (var stream = eventStore.CreateStream(StreamId))
			{
				stream.Add(new SomeDomainEvent { Value = "Initial event." });
				stream.CommitChanges(Guid.NewGuid(), null);
			}
		}

		private static void AppendToStream()
		{
			using (var stream = eventStore.OpenStream(StreamId, int.MinValue, int.MaxValue))
			{
				stream.Add(new SomeDomainEvent { Value = "Second event." });
				stream.CommitChanges(Guid.NewGuid(), null);
			}
		}

		private static void TakeSnapshot()
		{
			var memento = new AggregateMemento { Value = "snapshot" };
			eventStore.AddSnapshot(new Snapshot(StreamId, 2, memento));
		}

		private static void LoadFromSnapshotForwardAndAppend()
		{
			var latestSnapshot = eventStore.GetSnapshot(StreamId, int.MaxValue);

			using (var stream = eventStore.OpenStream(latestSnapshot, int.MaxValue))
			{
				stream.Add(new SomeDomainEvent { Value = "Third event (first one after a snapshot)." });
				stream.CommitChanges(Guid.NewGuid(), null);
			}
		}
	}

	internal class SomeDomainEvent
	{
		public string Value { get; set; }
	}
	internal class AggregateMemento
	{
		public string Value { get; set; }
	}
}