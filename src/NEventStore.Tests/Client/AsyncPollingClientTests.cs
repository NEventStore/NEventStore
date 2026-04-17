#pragma warning disable IDE1006 // Naming Styles

using System.Diagnostics;
using FakeItEasy;
using NEventStore.Persistence;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD;
using FluentAssertions;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

namespace NEventStore.PollingClient.Async
{
#if MSTEST
    [TestClass]
#endif
    public class Creating_AsyncPollingClient_Tests
    {
        [Fact]
        public void When_persist_streams_is_null_then_should_throw()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Catch.Exception(() => new AsyncPollingClient(null, new CommitStreamObserver()).Should().BeOfType<ArgumentNullException>());
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void When_interval_less_than_zero_then_should_throw()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Catch.Exception(() => new AsyncPollingClient(A.Fake<IPersistStreams>(), null)).Should().BeOfType<ArgumentNullException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class base_handling_committed_events : using_AsyncPollingClient
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            Observer = new LambdaAsyncObserver<ICommit>((c, _) => { commits.Add(c); return Task.FromResult(true); });
            base.Context();
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Because()
        {
            Sut.Start(0);
        }

        [Fact]
        public void commits_are_correctly_dispatched()
        {
            WaitForCondition(() => commits.Count >= 1);
            commits.Count.Should().Be(1);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class base_handling_committed_events_and_new_events : using_AsyncPollingClient
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            Observer = new LambdaAsyncObserver<ICommit>((c, _) => { commits.Add(c); return Task.FromResult(true); });
            base.Context();
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Because()
        {
            Sut.Start(0);
            for (int i = 0; i < 15; i++)
            {
                StoreEvents.Advanced.CommitSingle();
            }
        }

        [Fact]
        public void commits_are_correctly_dispatched()
        {
            WaitForCondition(() => commits.Count >= 16);
            commits.Count.Should().Be(16);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class verify_stopping_commit_polling_client : using_AsyncPollingClient
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            Observer = new LambdaAsyncObserver<ICommit>((c, _) => { commits.Add(c); return Task.FromResult(false); });
            base.Context();
            StoreEvents.Advanced.CommitSingle();
            StoreEvents.Advanced.CommitSingle();
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Because()
        {
            Sut.Start(0);
        }

        [Fact]
        public void commits_are_correctly_dispatched()
        {
            WaitForCondition(() => commits.Count >= 2, timeoutInSeconds: 1);
            commits.Count.Should().Be(1);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class verify_manual_polling : using_AsyncPollingClient
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            Observer = new LambdaAsyncObserver<ICommit>((c, _) => { commits.Add(c); return Task.FromResult(true); });
            base.Context();
            StoreEvents.Advanced.CommitSingle();
            StoreEvents.Advanced.CommitSingle();
        }

        protected override Task BecauseAsync()
        {
            Sut.ConfigurePollingClient();
            return Sut.PollAsync(CancellationToken.None);
        }

        [Fact]
        public void commits_are_retried_then_move_next()
        {
            WaitForCondition(() => commits.Count >= 2, timeoutInSeconds: 3);
            commits.Count.Should().Be(2);
            commits
                .Select(c => c.CheckpointToken)
                .SequenceEqual([1L, 2L])
                .Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_polling_finds_commits_and_more_data_is_immediately_available : using_AsyncPollingClient_with_fake_persistence
    {
        private readonly List<ICommit> _commits = [];
        private readonly List<long> _pollStartTimesInMilliseconds = [];
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        protected override void Context()
        {
            Observer = new LambdaAsyncObserver<ICommit>((commit, _) =>
            {
                _commits.Add(commit);
                return Task.FromResult(true);
            });

            base.Context();

            A.CallTo(() => Persistence.GetFromAsync(A<long>._, A<IAsyncObserver<ICommit>>._, A<CancellationToken>._))
                .ReturnsLazily((long checkpointToken, IAsyncObserver<ICommit> observer, CancellationToken cancellationToken) =>
                {
                    lock (_pollStartTimesInMilliseconds)
                    {
                        _pollStartTimesInMilliseconds.Add(_stopwatch.ElapsedMilliseconds);
                    }

                    return checkpointToken switch
                    {
                        0 => DeliverCommitAsync(observer, 1, cancellationToken),
                        1 => DeliverCommitAsync(observer, 2, cancellationToken),
                        _ => CompleteAsync(observer, cancellationToken)
                    };
                });
        }

        protected override void Because()
        {
            Sut.Start(0);
        }

        [Fact]
        public void should_repoll_without_waiting_for_the_idle_interval_after_progress()
        {
            WaitForCondition(() => _commits.Count >= 2 && _pollStartTimesInMilliseconds.Count >= 2, timeoutInSeconds: 1);

            _commits.Select(c => c.CheckpointToken).Should().Equal([1L, 2L]);
            _pollStartTimesInMilliseconds[1].Should().BeLessThan(PollingInterval);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_stopping_during_a_blocking_poll : using_AsyncPollingClient_with_fake_persistence
    {
        private readonly TaskCompletionSource<bool> _pollStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TimeSpan _stopDuration;

        protected override void Context()
        {
            base.Context();

            A.CallTo(() => Persistence.GetFromAsync(A<long>._, A<IAsyncObserver<ICommit>>._, A<CancellationToken>._))
                .ReturnsLazily(async (long _, IAsyncObserver<ICommit> _, CancellationToken cancellationToken) =>
                {
                    _pollStarted.TrySetResult(true);
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                });
        }

        protected override async Task BecauseAsync()
        {
            Sut.Start(0);
            await _pollStarted.Task.ConfigureAwait(false);

            var stopwatch = Stopwatch.StartNew();
            await Sut.StopAsync().ConfigureAwait(false);
            stopwatch.Stop();
            _stopDuration = stopwatch.Elapsed;
        }

        [Fact]
        public void should_wait_for_the_polling_worker_to_finish_observing_cancellation()
        {
            _stopDuration.Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void should_not_start_another_poll_after_stop_completes()
        {
            A.CallTo(() => Persistence.GetFromAsync(A<long>._, A<IAsyncObserver<ICommit>>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_disposing_during_a_blocking_poll : using_AsyncPollingClient_with_fake_persistence
    {
        private readonly TaskCompletionSource<bool> _pollStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TimeSpan _disposeDuration;

        protected override void Context()
        {
            base.Context();

            A.CallTo(() => Persistence.GetFromAsync(A<long>._, A<IAsyncObserver<ICommit>>._, A<CancellationToken>._))
                .ReturnsLazily(async (long _, IAsyncObserver<ICommit> _, CancellationToken cancellationToken) =>
                {
                    _pollStarted.TrySetResult(true);
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                });
        }

        protected override async Task BecauseAsync()
        {
            Sut.Start(0);
            await _pollStarted.Task.ConfigureAwait(false);

            var stopwatch = Stopwatch.StartNew();
            await Task.Run(() => Sut.Dispose()).ConfigureAwait(false);
            stopwatch.Stop();
            _disposeDuration = stopwatch.Elapsed;
        }

        [Fact]
        public void should_wait_for_the_polling_worker_to_finish_before_returning()
        {
            _disposeDuration.Should().BeLessThan(TimeSpan.FromSeconds(1));
        }
    }

    public abstract class using_AsyncPollingClient : SpecificationBase
    {
        protected const int PollingInterval = 100;
        protected AsyncPollingClient? sut;
        private IStoreEvents? _storeEvents;

        protected AsyncPollingClient Sut
        {
            get { return sut!; }
        }

        protected IStoreEvents StoreEvents
        {
            get { return _storeEvents!; }
        }

        protected IAsyncObserver<ICommit> Observer { get; set; } = new CommitStreamObserver();

        protected override void Context()
        {
            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            sut = new AsyncPollingClient(_storeEvents.Advanced, Observer, PollingInterval);
        }

        protected override void Cleanup()
        {
            _storeEvents?.Dispose();
            Sut.Dispose();
        }

        protected void WaitForCondition(Func<Boolean> predicate, Int32 timeoutInSeconds = 4)
        {
            DateTime startTest = DateTime.Now;
            while (!predicate() && DateTime.Now.Subtract(startTest).TotalSeconds < timeoutInSeconds)
            {
                Thread.Sleep(100);
            }
        }
    }

    public abstract class using_AsyncPollingClient_with_fake_persistence : SpecificationBase
    {
        protected const int PollingInterval = 500;
        protected AsyncPollingClient? sut;

        protected AsyncPollingClient Sut
        {
            get { return sut!; }
        }

        protected IPersistStreams Persistence { get; private set; } = null!;

        protected IAsyncObserver<ICommit> Observer { get; set; } = new CommitStreamObserver();

        protected override void Context()
        {
            Persistence = A.Fake<IPersistStreams>();
            sut = new AsyncPollingClient(Persistence, Observer, PollingInterval);
        }

        protected override void Cleanup()
        {
            sut?.Dispose();
        }

        protected void WaitForCondition(Func<Boolean> predicate, Int32 timeoutInSeconds = 4)
        {
            DateTime startTest = DateTime.Now;
            while (!predicate() && DateTime.Now.Subtract(startTest).TotalSeconds < timeoutInSeconds)
            {
                Thread.Sleep(25);
            }
        }

        protected static async Task DeliverCommitAsync(IAsyncObserver<ICommit> observer, long checkpointToken, CancellationToken cancellationToken)
        {
            await observer.OnNextAsync(BuildCommit(checkpointToken), cancellationToken).ConfigureAwait(false);
            await observer.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
        }

        protected static Task CompleteAsync(IAsyncObserver<ICommit> observer, CancellationToken cancellationToken)
        {
            return observer.OnCompletedAsync(cancellationToken);
        }

        protected static ICommit BuildCommit(long checkpointToken)
        {
            return new Commit(
                Bucket.Default,
                "polling-stream",
                (int)checkpointToken,
                Guid.NewGuid(),
                (int)checkpointToken,
                DateTime.UtcNow,
                checkpointToken,
                null,
                [new EventMessage { Body = $"event-{checkpointToken}" }]);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
