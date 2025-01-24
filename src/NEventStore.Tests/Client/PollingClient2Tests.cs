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

namespace NEventStore.PollingClient
{
#if MSTEST
    [TestClass]
#endif
    public class CreatingPollingClient2Tests
    {
        [Fact]
        public void When_persist_streams_is_null_then_should_throw()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Catch.Exception(() => new PollingClient2(null, _ => PollingClient2.HandlingResult.MoveToNext)).Should().BeOfType<ArgumentNullException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void When_interval_less_than_zero_then_should_throw()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Catch.Exception(() => new PollingClient2(A.Fake<IPersistStreams>(), null)).Should().BeOfType<ArgumentNullException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class base_handling_committed_events : using_polling_client2
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            base.Context();
            HandleFunction = c =>
            {
                commits.Add(c);
                return PollingClient2.HandlingResult.MoveToNext;
            };
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Because()
        {
            Sut.StartFrom(0);
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
    public class base_handling_committed_events_and_new_events : using_polling_client2
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            base.Context();
            HandleFunction = c =>
            {
                commits.Add(c);
                return PollingClient2.HandlingResult.MoveToNext;
            };
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Because()
        {
            Sut.StartFrom(0);
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
    public class verify_stopping_commit_polling_client : using_polling_client2
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            base.Context();
            HandleFunction = c =>
            {
                commits.Add(c);
                return PollingClient2.HandlingResult.Stop;
            };
            StoreEvents.Advanced.CommitSingle();
            StoreEvents.Advanced.CommitSingle();
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Because()
        {
            Sut.StartFrom(0);
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
    public class verify_retry_commit_polling_client : using_polling_client2
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            base.Context();
            HandleFunction = c =>
            {
                commits.Add(c);
                if (commits.Count < 3)
                    return PollingClient2.HandlingResult.Retry;

                return PollingClient2.HandlingResult.MoveToNext;
            };
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Because()
        {
            Sut.StartFrom(0);
        }

        [Fact]
        public void commits_are_retried()
        {
            WaitForCondition(() => commits.Count >= 3, timeoutInSeconds: 1);
            commits.Count.Should().Be(3);
            commits.All(c => c.CheckpointToken == 1).Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class verify_retry_then_move_next : using_polling_client2
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            base.Context();
            HandleFunction = c =>
            {
                commits.Add(c);
                if (commits.Count < 3 && c.CheckpointToken == 1)
                    return PollingClient2.HandlingResult.Retry;

                return PollingClient2.HandlingResult.MoveToNext;
            };
            StoreEvents.Advanced.CommitSingle();
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Because()
        {
            Sut.StartFrom(0);
        }

        [Fact]
        public void commits_are_retried_then_move_next()
        {
            WaitForCondition(() => commits.Count >= 4, timeoutInSeconds: 1);
            commits.Count.Should().Be(4);
            commits
                .Select(c => c.CheckpointToken)
                .SequenceEqual([1L, 1L, 1, 2])
                .Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class verify_manual_polling : using_polling_client2
    {
        private readonly List<ICommit> commits = [];

        protected override void Context()
        {
            base.Context();
            HandleFunction = c =>
            {
                commits.Add(c);
                return PollingClient2.HandlingResult.MoveToNext;
            };
            StoreEvents.Advanced.CommitSingle();
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Because()
        {
            Sut.ConfigurePollingFunction();
            Sut.PollNow();
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

    public abstract class using_polling_client2 : SpecificationBase
    {
        protected const int PollingInterval = 100;
        protected PollingClient2? sut;
        private IStoreEvents? _storeEvents;

        protected PollingClient2 Sut
        {
            get { return sut!; }
        }

        protected IStoreEvents StoreEvents
        {
            get { return _storeEvents!; }
        }

        protected Func<ICommit, PollingClient2.HandlingResult>? HandleFunction;

        protected override void Context()
        {
            HandleFunction = _ => PollingClient2.HandlingResult.MoveToNext;
            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            sut = new PollingClient2(_storeEvents.Advanced, c => HandleFunction(c), PollingInterval);
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
