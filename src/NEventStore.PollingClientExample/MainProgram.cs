namespace NEventStore.PollingClientExample
{
    using System;
    using NEventStore.Client;

    internal static class MainProgram
    {
        private static readonly byte[] EncryptionKey = { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };

        private static void Main()
        {
            using (var store = WireupEventStore())
            {
                Int64 checkpointToken = LoadCheckpoint();
                var client = new PollingClient2(store.Advanced, commit =>
                {
                    // Project the commit etc
                    Console.WriteLine(Resources.CommitInfo, commit.BucketId, commit.StreamId, commit.CommitSequence);
                    // Track the most recent checkpoint
                    checkpointToken = commit.CheckpointToken;
                    return PollingClient2.HandlingResult.MoveToNext;
                });

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
            return
                Wireup.Init()
                    .LogToOutputWindow()
                    .UsingInMemoryPersistence()
                        .InitializeStorageEngine()
                        .TrackPerformanceInstance("example")
                    .Build();
        }
    }
}