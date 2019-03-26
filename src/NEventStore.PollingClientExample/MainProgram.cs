namespace NEventStore.PollingClientExample
{
    using System;
    using NEventStore.Client;
    using NEventStore.Logging;

    internal static class MainProgram
    {
        private static readonly byte[] EncryptionKey = { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };

        private static void Main()
        {
            using (var store = WireupEventStore())
            {
                // append some commits to the EventStore
                AppendToStream(store, "Stream1");
                AppendToStream(store, "Stream2");
                AppendToStream(store, "Stream1");

                // now test the polling client
                Int64 checkpointToken = LoadCheckpoint();
                var client = new PollingClient2(store.Advanced, commit =>
                {
                    // Project the commit etc
                    Console.WriteLine(Resources.CommitInfo, commit.BucketId, commit.StreamId, commit.CommitSequence);
                    // Track the most recent checkpoint
                    checkpointToken = commit.CheckpointToken;
                    return PollingClient2.HandlingResult.MoveToNext;
                },
                waitInterval: 3000);

                client.StartFrom(checkpointToken);

                Console.WriteLine(Resources.PressAnyKey);
                Console.ReadKey();
                client.Stop();
                SaveCheckpoint(checkpointToken);
            }
        }

        private static Int64 LoadCheckpoint()
        {
            // Load the checkpoint value from disk / local db/ etc
            return 0;
        }

        private static void SaveCheckpoint(Int64 checkpointToken)
        {
            //Save checkpointValue to disk / whatever.
        }

        private static IStoreEvents WireupEventStore()
        {
            return Wireup.Init()
               .LogToOutputWindow(LogLevel.Verbose)
               .LogToConsoleWindow(LogLevel.Verbose)
               .UsingInMemoryPersistence()
               .InitializeStorageEngine()
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
               .TrackPerformanceInstance("example")
#endif
               .Build();
        }

        private static void AppendToStream(IStoreEvents store, string streamId)
        {
            using (var stream = store.OpenStream(streamId))
            {
                var @event = new SomeDomainEvent { Value = "event" };

                stream.Add(new EventMessage { Body = @event });
                stream.CommitChanges(Guid.NewGuid());
            }
        }
    }
}