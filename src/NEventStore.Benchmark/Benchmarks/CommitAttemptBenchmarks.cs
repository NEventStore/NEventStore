using BenchmarkDotNet.Attributes;
using NEventStore.Benchmark.Support;

namespace NEventStore.Benchmark.Benchmarks
{
    [Config(typeof(AllowNonOptimized))]
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [MemoryDiagnoser]
    [MeanColumn, StdErrorColumn, StdDevColumn, MinColumn, MaxColumn, IterationsColumn]
    public class CommitAttemptBenchmarks
    {
        private static readonly DateTime CommitStamp = new(2026, 04, 16, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid CommitId = new("e63c1cc7-7fe7-47ee-8a9d-c62093e6c755");
        private KeyValuePair<string, object>[] _headerEntries = [];
        private EventMessage[] _events = [];

        [Params(0, 5)]
        public int HeaderCount { get; set; }

        [Params(1, 10, 100)]
        public int EventsPerCommit { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _headerEntries = Enumerable.Range(0, HeaderCount)
                .Select(i => new KeyValuePair<string, object>($"header-{i}", $"value-{i}"))
                .ToArray();

            _events = Enumerable.Range(0, EventsPerCommit)
                .Select(i => new EventMessage { Body = new SomeDomainEvent { Value = i.ToString() } })
                .ToArray();
        }

        [Benchmark]
        public CommitAttempt ConstructCommitAttempt()
        {
            var headers = new Dictionary<string, object>(HeaderCount);
            foreach (var header in _headerEntries)
            {
                headers.Add(header.Key, header.Value);
            }

            var events = new EventMessage[_events.Length];
            Array.Copy(_events, events, _events.Length);

            return new CommitAttempt(
                Bucket.Default,
                "benchmark-stream",
                events.Length,
                CommitId,
                1,
                CommitStamp,
                headers,
                events);
        }
    }
}
