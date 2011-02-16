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

            OpenOrCreateStream();
            AppendToStream();
            TakeSnapshot();
            LoadFromSnapshotForwardAndAppend();
        }

        private static void OpenOrCreateStream()
        {
            // we can call CreateStream(StreamId) if we know there isn't going to be any data.
            // or we can call OpenStream(StreamId, 0, int.MaxValue) to read all commits,
            // if no commits exist then it creates a new stream for us.
            using (var stream = eventStore.OpenStream(StreamId, 0, int.MaxValue))
            {
                var @event = new SomeDomainEvent { Value = "Initial event." };

                stream.Add(new EventMessage { Body = @event });
                stream.CommitChanges(Guid.NewGuid());
            }
        }

        private static void AppendToStream()
        {
            using (var stream = eventStore.OpenStream(StreamId, int.MinValue, int.MaxValue))
            {
                var @event = new SomeDomainEvent { Value = "Second event." };

                stream.Add(new EventMessage { Body = @event });
                stream.CommitChanges(Guid.NewGuid());
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
			    var @event = new SomeDomainEvent {Value = "Third event (first one after a snapshot)."};
				
                stream.Add(new EventMessage { Body = @event });
				stream.CommitChanges(Guid.NewGuid());
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