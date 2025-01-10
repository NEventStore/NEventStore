using NEventStore.PollingClient;
using Microsoft.Extensions.Logging;

namespace NEventStore.PollingClientExample
{
    internal static class MainProgram
    {
        private static void Main()
        {
            using var store = WireupEventStore();
            // append some commits to the EventStore
            AppendToStream(store, "Stream1");
            AppendToStream(store, "Stream2");
            AppendToStream(store, "Stream1");

            Console.WriteLine("--------------------------");
            Console.WriteLine("Starting PollingClient2...");
            Console.WriteLine();

            // now test the polling client
            Int64 checkpointToken = LoadCheckpoint();
            var client = new PollingClient2(store.Advanced, commit =>
            {
                // Project the commit etc
                Console.WriteLine("BucketId={0};StreamId={1};CommitSequence={2}", commit.BucketId, commit.StreamId, commit.CommitSequence);
                // Track the most recent checkpoint
                checkpointToken = commit.CheckpointToken;
                return PollingClient2.HandlingResult.MoveToNext;
            },
            waitInterval: 3000);

            client.StartFrom(checkpointToken);
            Console.WriteLine("Wait for the Stream to end, then press any key to continue...");
            Console.ReadKey();
            client.Stop();

            Console.WriteLine();
            Console.WriteLine("------------------------------");
            Console.WriteLine("Starting AsyncPollingClient...");
            Console.WriteLine();

            checkpointToken = LoadCheckpoint();
            var observer = new LambdaAsyncObserver<ICommit>((commit, _) =>
            {
                // Project the commit etc
                Console.WriteLine("BucketId={0};StreamId={1};CommitSequence={2}", commit.BucketId, commit.StreamId, commit.CommitSequence);
                // Track the most recent checkpoint
                checkpointToken = commit.CheckpointToken;
                return Task.FromResult(true);
            });
            var asyncClient = new AsyncPollingClient(store.Advanced, observer, waitInterval: 3000, holeDetectionWaitInterval: 100);
            asyncClient.Start(checkpointToken);

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            asyncClient.StopAsync()
                .GetAwaiter().GetResult();
        }

        private static Int64 LoadCheckpoint()
        {
            // Load the checkpoint value from disk / local db/ etc
            return 0;
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
               .UsingInMemoryPersistence()
               .InitializeStorageEngine()
#if NET462
               .TrackPerformanceInstance("example")
#endif
               .Build();
        }

        private static void AppendToStream(IStoreEvents store, string streamId)
        {
            using var stream = store.OpenStream(streamId);
            var @event = new SomeDomainEvent { Value = "event" };

            stream.Add(new EventMessage { Body = @event });
            stream.CommitChanges(Guid.NewGuid());
        }
    }
}