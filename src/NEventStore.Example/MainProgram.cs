using Microsoft.Extensions.Logging;

namespace NEventStore.Example
{
    internal static class MainProgram
    {
        private static readonly Guid StreamId = Guid.NewGuid(); // aggregate identifier

        private static IStoreEvents? store;

        private static void Main()
        {
            // Console.WindowWidth = Console.LargestWindowWidth - 20;

            Console.WriteLine("------------------");
            Console.WriteLine("Using Sync Methods");
            Console.WriteLine("------------------");
            Console.WriteLine();

            using (store = WireupEventStore())
            {
                OpenOrCreateStream();
                AppendToStream();
                TakeSnapshot();
                LoadFromSnapshotForwardAndAppend();
            }

            Console.WriteLine();
            Console.WriteLine("-------------------");
            Console.WriteLine("Using Async Methods");
            Console.WriteLine("-------------------");
            Console.WriteLine();

            Task.Run(async () =>
            {
                using (store = WireupEventStore())
                {
                    await OpenOrCreateStreamAsync();
                    await AppendToStreamAsync();
                    await TakeSnapshotAsync();
                    await LoadFromSnapshotForwardAndAppendAsync();
                }
            }).GetAwaiter().GetResult();

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static IStoreEvents WireupEventStore()
        {
            var loggerFactory = LoggerFactory.Create(logging =>
            {
                logging
                    .AddConsole()
                    .AddDebug()
                    .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            });

            return Wireup.Init()
               .WithLoggerFactory(loggerFactory)
               .UseOptimisticPipelineHook()
               .UsingInMemoryPersistence()
               .InitializeStorageEngine()
#if NET462
               .TrackPerformanceInstance("example")
#endif
               .HookIntoPipelineUsing(new AuthorizationPipelineHook())
               .Build();
        }

        #region Sync Methods

        private static void OpenOrCreateStream()
        {
            // we can call CreateStream(StreamId) if we know there isn't going to be any data.
            // or we can call OpenStream(StreamId, 0, int.MaxValue) to read all commits,
            // if no commits exist then it creates a new stream for us.
            using var stream = store!.OpenStream(StreamId, 0, int.MaxValue);
            var @event = new SomeDomainEvent { Value = "Initial event." };

            stream.Add(new EventMessage { Body = @event });
            stream.CommitChanges(Guid.NewGuid());
        }

        private static void AppendToStream()
        {
            using var stream = store!.OpenStream(StreamId);
            var @event = new SomeDomainEvent { Value = "Second event." };

            stream.Add(new EventMessage { Body = @event });
            stream.CommitChanges(Guid.NewGuid());
        }

        private static void TakeSnapshot()
        {
            var memento = new AggregateMemento { Value = "snapshot" };
            store!.Advanced.AddSnapshot(new Snapshot(StreamId.ToString(), 2, memento));
        }

        private static void LoadFromSnapshotForwardAndAppend()
        {
            var latestSnapshot = store!.Advanced.GetSnapshot(StreamId, int.MaxValue)
                ?? throw new InvalidOperationException("No snapshot found.");

            using var stream = store.OpenStream(latestSnapshot, int.MaxValue);
            var @event = new SomeDomainEvent { Value = "Third event (first one after a snapshot)." };

            stream.Add(new EventMessage { Body = @event });
            stream.CommitChanges(Guid.NewGuid());
        }

        #endregion

        #region Async Methods

        private static async Task OpenOrCreateStreamAsync()
        {
            // we can call CreateStream(StreamId) if we know there isn't going to be any data.
            // or we can call OpenStream(StreamId, 0, int.MaxValue) to read all commits,
            // if no commits exist then it creates a new stream for us.
            using var stream = await store!.OpenStreamAsync(StreamId, 0, int.MaxValue, cancellationToken: CancellationToken.None);
            var @event = new SomeDomainEvent { Value = "Initial event." };

            stream.Add(new EventMessage { Body = @event });
            await stream.CommitChangesAsync(Guid.NewGuid(), CancellationToken.None);
        }

        private static async Task AppendToStreamAsync()
        {
            using var stream = await store!.OpenStreamAsync(StreamId, cancellationToken: CancellationToken.None);
            var @event = new SomeDomainEvent { Value = "Second event." };

            stream.Add(new EventMessage { Body = @event });
            await stream.CommitChangesAsync(Guid.NewGuid(), CancellationToken.None);
        }

        private static Task<bool> TakeSnapshotAsync()
        {
            var memento = new AggregateMemento { Value = "snapshot" };
            return store!.Advanced.AddSnapshotAsync(new Snapshot(StreamId.ToString(), 2, memento), CancellationToken.None);
        }

        private static async Task LoadFromSnapshotForwardAndAppendAsync()
        {
            var latestSnapshot = await store!.Advanced.GetSnapshotAsync(StreamId, int.MaxValue, CancellationToken.None)
                ?? throw new InvalidOperationException("No snapshot found.");

            using var stream = await store.OpenStreamAsync(latestSnapshot, int.MaxValue, CancellationToken.None);
            var @event = new SomeDomainEvent { Value = "Third event (first one after a snapshot)." };

            stream.Add(new EventMessage { Body = @event });
            await stream.CommitChangesAsync(Guid.NewGuid(), CancellationToken.None);
        }

        #endregion
    }
}