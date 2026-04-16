using BenchmarkDotNet.Attributes;
using NEventStore.Benchmark.Support;

namespace NEventStore.Benchmark.Benchmarks
{
    [Config(typeof(AllowNonOptimized))]
    [SimpleJob(launchCount: 3, warmupCount: 3, iterationCount: 3, invocationCount: 1)]
    [MemoryDiagnoser]
    [MeanColumn, StdErrorColumn, StdDevColumn, MinColumn, MaxColumn, IterationsColumn]
    public class StreamWriteLargeBenchmarks
    {
        private const int StreamsPerInvocation = 8;
        private IStoreEvents? _eventStore;
        private PreGeneratedStreamData? _data;
        private string[] _streamIds = [];

        [Params(10000, 100000)]
        public int CommitCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _data = PreGeneratedStreamData.Create(CommitCount);
            _streamIds = Enumerable.Range(0, StreamsPerInvocation)
                .Select(i => $"write-large-benchmark-stream-{i:D2}")
                .ToArray();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _eventStore = EventStoreHelpers.WireupEventStore();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _eventStore?.Dispose();
            _eventStore = null;
        }

        [Benchmark(OperationsPerInvoke = StreamsPerInvocation)]
        public void AppendSingleEventCommits()
        {
            for (var streamIndex = 0; streamIndex < StreamsPerInvocation; streamIndex++)
            {
                using var stream = EventStore.CreateStream(_streamIds[streamIndex]);
                for (var i = 0; i < Data.CommitIds.Length; i++)
                {
                    stream.Add(new EventMessage { Body = Data.EventBodies[i] });
                    stream.CommitChanges(Data.CommitIds[i]);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = StreamsPerInvocation)]
        public async Task AppendSingleEventCommitsAsync()
        {
            for (var streamIndex = 0; streamIndex < StreamsPerInvocation; streamIndex++)
            {
                using var stream = await EventStore.OpenStreamAsync(_streamIds[streamIndex], 0, int.MaxValue, CancellationToken.None).ConfigureAwait(false);
                for (var i = 0; i < Data.CommitIds.Length; i++)
                {
                    stream.Add(new EventMessage { Body = Data.EventBodies[i] });
                    await stream.CommitChangesAsync(Data.CommitIds[i], CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        private IStoreEvents EventStore => _eventStore ?? throw new InvalidOperationException("Benchmark store has not been initialized.");

        private PreGeneratedStreamData Data => _data ?? throw new InvalidOperationException("Benchmark data has not been initialized.");
    }
}
