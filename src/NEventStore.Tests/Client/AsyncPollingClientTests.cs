#pragma warning disable IDE1006 // Naming Styles

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
}

#pragma warning restore IDE1006 // Naming Styles
