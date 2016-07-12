namespace NEventStore.PollingClientExample
{
    using System;
    using NEventStore.Client;

    internal static class MainProgram
    {
        private static readonly byte[] EncryptionKey = {0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf};

        private static void Main()
        {
            using (var store = WireupEventStore())
            {
                var client = new PollingClient(store.Advanced);
                Int64 checkpointToken = LoadCheckpoint();
                using (IObserveCommits observeCommits = client.ObserveFrom(checkpointToken))
                using (observeCommits.Subscribe(commit =>
                {
                    // Project the commit etc
                    Console.WriteLine(Resources.CommitInfo, commit.BucketId, commit.StreamId, commit.CommitSequence);
                    // Track the most recent checkpoint
                    checkpointToken = commit.CheckpointToken;
                }))
                {
                    observeCommits.Start();

                    Console.WriteLine(Resources.PressAnyKey);
                    Console.ReadKey();

                    SaveCheckpoint(checkpointToken);
                }
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
                        .UsingJsonSerialization()
                        .Compress()
                        .EncryptWith(EncryptionKey)
                    .Build();
        }
    }
}