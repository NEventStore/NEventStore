using BenchmarkDotNet.Attributes;
using NEventStore.Benchmark.Support;
using System;

namespace NEventStore.Benchmark.Benchmarks
{
    [Config(typeof(AllowNonOptimized))]
    [SimpleJob(launchCount: 3, warmupCount: 3, targetCount: 3, invocationCount: 1)]
    [MemoryDiagnoser]
    [MeanColumn, StdErrorColumn, StdDevColumn, MinColumn, MaxColumn, IterationsColumn]
    public class PersistenceBenchmarks
    {
        private static readonly Guid StreamId = Guid.NewGuid(); // aggregate identifier
        private readonly IStoreEvents _eventStore;

        public PersistenceBenchmarks()
        {
            _eventStore = EventStoreHelpers.WireupEventStore();
        }

        [Params(100, 1000, 10000, 100000)]
        public int CommitsToWrite { get; set; }

        [Benchmark]
        public void WriteToStream()
        {
            // we can call CreateStream(StreamId) if we know there isn't going to be any data.
            // or we can call OpenStream(StreamId, 0, int.MaxValue) to read all commits,
            // if no commits exist then it creates a new stream for us.
            using (var stream = _eventStore.OpenStream(StreamId, 0, int.MaxValue))
            {
                for (int i = 0; i < CommitsToWrite; i++)
                {
                    var @event = new SomeDomainEvent { Value = i.ToString() };
                    stream.Add(new EventMessage { Body = @event });
                    stream.CommitChanges(Guid.NewGuid());
                }
            }
        }

        [GlobalSetup(Targets = new string[] { nameof(ReadFromStream), nameof(ReadFromEventStore) })]
        public void ReadSetup()
        {
            using (var stream = _eventStore.OpenStream(StreamId, 0, int.MaxValue))
            {
                for (int i = 0; i < CommitsToWrite; i++)
                {
                    var @event = new SomeDomainEvent { Value = i.ToString() };
                    stream.Add(new EventMessage { Body = @event });
                    stream.CommitChanges(Guid.NewGuid());
                }
            }
        }

        [Benchmark]
        public void ReadFromStream()
        {
            // we can call CreateStream(StreamId) if we know there isn't going to be any data.
            // or we can call OpenStream(StreamId, 0, int.MaxValue) to read all commits,
            // if no commits exist then it creates a new stream for us.
            using (var stream = _eventStore.OpenStream(StreamId, 0, int.MaxValue))
            {
                // the whole stream has been read
                // Console.WriteLine(stream.CommittedEvents.First().Body);
            }
        }

        [Benchmark]
        public void ReadFromEventStore()
        {
            var commits = _eventStore.Advanced.GetFrom(Bucket.Default, 0);
            foreach (var c in commits)
            {
                // just iterate through all the commits
                // Console.WriteLine(c);
            }
        }
    }
}
