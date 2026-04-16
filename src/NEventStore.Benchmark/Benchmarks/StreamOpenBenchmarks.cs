using BenchmarkDotNet.Attributes;
using NEventStore.Benchmark.Support;

namespace NEventStore.Benchmark.Benchmarks
{
    [Config(typeof(AllowNonOptimized))]
    [SimpleJob(launchCount: 3, warmupCount: 3, iterationCount: 3)]
    [MemoryDiagnoser]
    [MeanColumn, StdErrorColumn, StdDevColumn, MinColumn, MaxColumn, IterationsColumn]
    public class StreamOpenBenchmarks
    {
        private static readonly Guid StreamId = new("7f4f4d6c-0b32-4f56-a7e2-bf6d4f5f60ab");
        private IStoreEvents? _eventStore;
        private PreGeneratedStreamData? _data;

        [Params(100, 1000, 10000, 100000)]
        public int CommitCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _data = PreGeneratedStreamData.Create(CommitCount);
        }

        [IterationSetup(Target = nameof(OpenEmptyStream))]
        public void SetupEmptyStream()
        {
            _eventStore = EventStoreHelpers.WireupEventStore();
        }

        [IterationSetup(Target = nameof(OpenEmptyStreamAsync))]
        public void SetupEmptyStreamAsync()
        {
            _eventStore = EventStoreHelpers.WireupEventStore();
        }

        [IterationSetup(Target = nameof(OpenPopulatedStream))]
        public void SetupPopulatedStream()
        {
            _eventStore = EventStoreHelpers.WireupEventStore();
            SeedStore(_eventStore, Data);
        }

        [IterationSetup(Target = nameof(OpenPopulatedStreamAsync))]
        public void SetupPopulatedStreamAsync()
        {
            _eventStore = EventStoreHelpers.WireupEventStore();
            SeedStore(_eventStore, Data);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _eventStore?.Dispose();
            _eventStore = null;
        }

        [Benchmark]
        public void OpenEmptyStream()
        {
            using var stream = EventStore.OpenStream(StreamId, 0, int.MaxValue);
        }

        [Benchmark]
        public async Task OpenEmptyStreamAsync()
        {
            using var stream = await EventStore.OpenStreamAsync(StreamId, 0, int.MaxValue, CancellationToken.None).ConfigureAwait(false);
        }

        [Benchmark]
        public void OpenPopulatedStream()
        {
            using var stream = EventStore.OpenStream(StreamId, 0, int.MaxValue);
        }

        [Benchmark]
        public async Task OpenPopulatedStreamAsync()
        {
            using var stream = await EventStore.OpenStreamAsync(StreamId, 0, int.MaxValue, CancellationToken.None).ConfigureAwait(false);
        }

        private IStoreEvents EventStore => _eventStore ?? throw new InvalidOperationException("Benchmark store has not been initialized.");

        private PreGeneratedStreamData Data => _data ?? throw new InvalidOperationException("Benchmark data has not been initialized.");

        private static void SeedStore(IStoreEvents eventStore, PreGeneratedStreamData data)
        {
            using var stream = eventStore.CreateStream(StreamId);
            for (var i = 0; i < data.CommitIds.Length; i++)
            {
                stream.Add(new EventMessage { Body = data.EventBodies[i] });
                stream.CommitChanges(data.CommitIds[i]);
            }
        }
    }
}
