using System.Reflection;
using BenchmarkDotNet.Attributes;
using NEventStore.Benchmark.Support;
using NEventStore.Persistence;
using NEventStore.Persistence.InMemory;
using NEventStore.PollingClient;

namespace NEventStore.Benchmark.Benchmarks
{
    [Config(typeof(AllowNonOptimized))]
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
    [MemoryDiagnoser]
    [MeanColumn, StdErrorColumn, StdDevColumn, MinColumn, MaxColumn, IterationsColumn]
    public class AsyncPollingBenchmarks
    {
        private static readonly MethodInfo ConfigurePollingClientMethod =
            typeof(AsyncPollingClient).GetMethod("ConfigurePollingClient", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate AsyncPollingClient.ConfigurePollingClient");

        private InMemoryPersistenceEngine? _idleEngine;
        private InMemoryPersistenceEngine? _catchUpEngine;

        [Params(1000)]
        public int CommitCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _idleEngine = new InMemoryPersistenceEngine();
            _idleEngine.Initialize();

            _catchUpEngine = new InMemoryPersistenceEngine();
            _catchUpEngine.Initialize();

            for (var i = 0; i < CommitCount; i++)
            {
                _catchUpEngine.Commit(new CommitAttempt(
                    Bucket.Default,
                    $"polling-stream-{i % 32:D2}",
                    (i / 32) + 1,
                    Guid.NewGuid(),
                    (i / 32) + 1,
                    DateTime.UtcNow,
                    null,
                    [new EventMessage { Body = new SomeDomainEvent { Value = i.ToString() } }]));
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _idleEngine?.Dispose();
            _catchUpEngine?.Dispose();
            _idleEngine = null;
            _catchUpEngine = null;
        }

        [Benchmark]
        public async Task IdlePollAsync()
        {
            using var client = CreateClient(IdleEngine);
            await client.PollAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task CatchUpPollAsync()
        {
            using var client = CreateClient(CatchUpEngine);
            await client.PollAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private AsyncPollingClient CreateClient(IPersistStreams persistStreams)
        {
            var client = new AsyncPollingClient(persistStreams, new NoOpCommitObserver());
            ConfigurePollingClientMethod.Invoke(client, [0L, null]);
            return client;
        }

        private InMemoryPersistenceEngine IdleEngine => _idleEngine ?? throw new InvalidOperationException("Idle engine has not been initialized.");

        private InMemoryPersistenceEngine CatchUpEngine => _catchUpEngine ?? throw new InvalidOperationException("Catch-up engine has not been initialized.");

        private sealed class NoOpCommitObserver : IAsyncObserver<ICommit>
        {
            public Task OnCompletedAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task OnErrorAsync(Exception ex, CancellationToken cancellationToken = default)
            {
                throw ex;
            }

            public Task<bool> OnNextAsync(ICommit value, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(true);
            }
        }
    }
}
