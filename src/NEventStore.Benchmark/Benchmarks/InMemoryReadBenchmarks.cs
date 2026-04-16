using BenchmarkDotNet.Attributes;
using NEventStore.Benchmark.Support;
using NEventStore.Persistence.InMemory;

namespace NEventStore.Benchmark.Benchmarks
{
    [Config(typeof(AllowNonOptimized))]
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [MemoryDiagnoser]
    [MeanColumn, StdErrorColumn, StdDevColumn, MinColumn, MaxColumn, IterationsColumn]
    public class InMemoryReadBenchmarks
    {
        private const int StreamCount = 64;
        private readonly string[] _streamIds = Enumerable.Range(0, StreamCount).Select(i => $"stream-{i:D2}").ToArray();
        private InMemoryPersistenceEngine? _engine;
        private long _midpointCheckpoint;
        private string? _selectedStreamId;
        private int _selectedMinRevision;

        [Params(10000, 100000)]
        public int CommitCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            var streamRevisions = new int[StreamCount];
            var streamCommitSequences = new int[StreamCount];
            var midpointIndex = CommitCount / 2;

            for (var i = 0; i < CommitCount; i++)
            {
                var streamIndex = i % StreamCount;
                streamRevisions[streamIndex]++;
                streamCommitSequences[streamIndex]++;

                var commit = _engine.Commit(new CommitAttempt(
                    Bucket.Default,
                    _streamIds[streamIndex],
                    streamRevisions[streamIndex],
                    Guid.NewGuid(),
                    streamCommitSequences[streamIndex],
                    DateTime.UtcNow,
                    null,
                    [new EventMessage { Body = new SomeDomainEvent { Value = i.ToString() } }]));

                if (i == midpointIndex)
                {
                    _midpointCheckpoint = commit!.CheckpointToken;
                }
            }

            _selectedStreamId = _streamIds[0];
            _selectedMinRevision = Math.Max(1, (CommitCount / StreamCount) / 2);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _engine?.Dispose();
            _engine = null;
        }

        [Benchmark]
        public int ReadGlobalCheckpointRange()
        {
            var totalEvents = 0;
            foreach (var commit in Engine.GetFrom(_midpointCheckpoint))
            {
                totalEvents += commit.Events.Count;
            }

            return totalEvents;
        }

        [Benchmark]
        public int ReadBucketCheckpointRange()
        {
            var totalEvents = 0;
            foreach (var commit in Engine.GetFrom(Bucket.Default, _midpointCheckpoint))
            {
                totalEvents += commit.Events.Count;
            }

            return totalEvents;
        }

        [Benchmark]
        public int ReadStreamRevisionRange()
        {
            var totalEvents = 0;
            foreach (var commit in Engine.GetFrom(Bucket.Default, SelectedStreamId, _selectedMinRevision, int.MaxValue))
            {
                totalEvents += commit.Events.Count;
            }

            return totalEvents;
        }

        private InMemoryPersistenceEngine Engine => _engine ?? throw new InvalidOperationException("Benchmark engine has not been initialized.");

        private string SelectedStreamId => _selectedStreamId ?? throw new InvalidOperationException("Benchmark stream has not been initialized.");
    }
}
