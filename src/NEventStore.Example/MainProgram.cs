namespace NEventStore.Example
{
    using System;
    using NEventStore;
    using Logging;

    internal static class MainProgram
    {
        private static readonly Guid StreamId = Guid.NewGuid(); // aggregate identifier

        private static IStoreEvents store;

        private static void Main()
        {
            // Console.WindowWidth = Console.LargestWindowWidth - 20;

            using (store = WireupEventStore())
            {
                OpenOrCreateStream();
                AppendToStream();
                TakeSnapshot();
                LoadFromSnapshotForwardAndAppend();
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static IStoreEvents WireupEventStore()
        {
            return Wireup.Init()
               .LogToOutputWindow(LogLevel.Verbose)
               .LogToConsoleWindow(LogLevel.Verbose)
               .UseOptimisticPipelineHook()
               .UsingInMemoryPersistence()
               .InitializeStorageEngine()
#if NET461
               .TrackPerformanceInstance("example")
#endif
               .HookIntoPipelineUsing(new[] { new AuthorizationPipelineHook() })
               .Build();
        }

        private static void OpenOrCreateStream()
        {
            // we can call CreateStream(StreamId) if we know there isn't going to be any data.
            // or we can call OpenStream(StreamId, 0, int.MaxValue) to read all commits,
            // if no commits exist then it creates a new stream for us.
            using (var stream = store.OpenStream(StreamId, 0, int.MaxValue))
            {
                var @event = new SomeDomainEvent { Value = "Initial event." };

                stream.Add(new EventMessage { Body = @event });
                stream.CommitChanges(Guid.NewGuid());
            }
        }

        private static void AppendToStream()
        {
            using (var stream = store.OpenStream(StreamId))
            {
                var @event = new SomeDomainEvent { Value = "Second event." };

                stream.Add(new EventMessage { Body = @event });
                stream.CommitChanges(Guid.NewGuid());
            }
        }

        private static void TakeSnapshot()
        {
            var memento = new AggregateMemento { Value = "snapshot" };
            store.Advanced.AddSnapshot(new Snapshot(StreamId.ToString(), 2, memento));
        }

        private static void LoadFromSnapshotForwardAndAppend()
        {
            var latestSnapshot = store.Advanced.GetSnapshot(StreamId, int.MaxValue);

            using (var stream = store.OpenStream(latestSnapshot, int.MaxValue))
            {
                var @event = new SomeDomainEvent { Value = "Third event (first one after a snapshot)." };

                stream.Add(new EventMessage { Body = @event });
                stream.CommitChanges(Guid.NewGuid());
            }
        }
    }
}