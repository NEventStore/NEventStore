namespace NEventStore.Client
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;
    using Moq;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class CreatingPollingClientTests
    {
        [Fact]
        public void When_persist_streams_is_null_then_should_throw()
        {
            Catch.Exception(() => new PollingClient(null)).ShouldBeInstanceOf<ArgumentNullException>();
        }

        [Fact]
        public void When_interval_less_than_zero_then_should_throw()
        {
            Catch.Exception(() => new PollingClient(new Mock<IPersistStreams>().Object,-1)).ShouldBeInstanceOf<ArgumentException>();
        }

        [Fact]
        public void When_interval_is_zero_then_should_throw()
        {
            Catch.Exception(() => new PollingClient(new Mock<IPersistStreams>().Object, 0)).ShouldBeInstanceOf<ArgumentException>();
        }
    }

    public abstract class using_polling_client : SpecificationBase
    {
        protected const int PollingInterval = 100;
        private PollingClient _pollingClient;
        private IStoreEvents _storeEvents;

        protected PollingClient PollingClient
        {
            get { return _pollingClient; }
        }

        protected IStoreEvents StoreEvents
        {
            get { return _storeEvents; }
        }

        protected override void Context()
        {
            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            _pollingClient = new PollingClient(_storeEvents.Advanced, PollingInterval);
        }

        protected override void Cleanup()
        {
            _storeEvents.Dispose();
        }
    }

    public class when_commit_is_comitted_before_subscribing : using_polling_client
    {
        private IObserveCommits _observeCommits;
        private Task<ICommit> _commitObserved;

        protected override void Context()
        {
            base.Context();
            StoreEvents.Advanced.CommitSingle();
            _observeCommits = PollingClient.ObserveFromBegininng();
            _commitObserved = _observeCommits.FirstAsync().ToTask();
        }

        protected override void Because()
        {
            _observeCommits.Start();
        }

        protected override void Cleanup()
        {
            _observeCommits.Dispose();
        }

        [Fact]
        public void should_observe_commit()
        {
            _commitObserved.Wait(PollingInterval * 2).ShouldBe(true);
        }
    }

    public class when_commit_is_comitted_before_and_after_subscribing : using_polling_client
    {
        private IObserveCommits _observeCommits;
        private Task<ICommit> _twoCommitsObserved;

        protected override void Context()
        {
            base.Context();
            StoreEvents.Advanced.CommitSingle();
            _observeCommits = PollingClient.ObserveFromBegininng();
            _twoCommitsObserved = _observeCommits.Take(2).ToTask();
        }

        protected override void Because()
        {
            _observeCommits.Start();
            StoreEvents.Advanced.CommitSingle();
        }

        protected override void Cleanup()
        {
            _observeCommits.Dispose();
        }

        [Fact]
        public void should_observe_two_commits()
        {
            _twoCommitsObserved.Wait(PollingInterval * 2).ShouldBe(true);
        }
    }

    public class with_two_observers_and_multiple_commits : using_polling_client
    {
        private IObserveCommits _observeCommits1;
        private IObserveCommits _observeCommits2;
        private Task<ICommit> _observeCommits1Complete;
        private Task<ICommit> _observeCommits2Complete;

        protected override void Context()
        {
            base.Context();
            StoreEvents.Advanced.CommitSingle();
            _observeCommits1 = PollingClient.ObserveFromBegininng();
            _observeCommits1Complete = _observeCommits1.Take(5).ToTask();

            _observeCommits2 = PollingClient.ObserveFromBegininng();
            _observeCommits2Complete = _observeCommits1.Take(10).ToTask();
        }

        protected override void Because()
        {
            _observeCommits1.Start();
            _observeCommits2.Start();
            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 15; i++)
                {
                    StoreEvents.Advanced.CommitSingle();
                }
            });
        }

        protected override void Cleanup()
        {
            _observeCommits1.Dispose();
            _observeCommits2.Dispose();
        }

        [Fact]
        public void should_observe_commits_on_first_observer()
        {
            _observeCommits1Complete.Wait(PollingInterval * 10).ShouldBe(true);
        }

        [Fact]
        public void should_observe_commits_on_second_observer()
        {
            _observeCommits2Complete.Wait(PollingInterval * 10).ShouldBe(true);
        }
    }

    public class with_two_subscriptions_on_a_single_observer_and_multiple_commits : using_polling_client
    {
        private IObserveCommits _observeCommits1;
        private Task<ICommit> _observeCommits1Complete;
        private Task<ICommit> _observeCommits2Complete;

        protected override void Context()
        {
            base.Context();
            StoreEvents.Advanced.CommitSingle();
            _observeCommits1 = PollingClient.ObserveFromBegininng();
            _observeCommits1Complete = _observeCommits1.Take(5).ToTask();
            _observeCommits2Complete = _observeCommits1.Take(10).ToTask();
        }

        protected override void Because()
        {
            _observeCommits1.Start();
            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < 15; i++)
                {
                    StoreEvents.Advanced.CommitSingle();
                }
            });
        }

        protected override void Cleanup()
        {
            _observeCommits1.Dispose();
        }

        [Fact]
        public void should_observe_commits_on_first_observer()
        {
            _observeCommits1Complete.Wait(PollingInterval * 10).ShouldBe(true);
        }

        [Fact]
        public void should_observe_commits_on_second_observer()
        {
            _observeCommits2Complete.Wait(PollingInterval * 10).ShouldBe(true);
        }
    }

    public class with_exception_when_handling_commit : using_polling_client
    {
        private IObserveCommits _observeCommits;
        private IDisposable _subscription;
        private Task _observingCommits;
        private Exception _subscriberException;
        private Exception _exception;
        private Exception _onErrorException;

        protected override void Context()
        {
            base.Context();
            StoreEvents.Advanced.CommitSingle();
            _observeCommits = PollingClient.ObserveFromBegininng();
            _subscriberException = new Exception();
            _subscription = _observeCommits.Subscribe(c => { throw _subscriberException; }, ex => _onErrorException = ex);
        }

        protected override void Because()
        {
            _observingCommits = _observeCommits.Start();
            StoreEvents.Advanced.CommitSingle();
            _exception = Catch.Exception(() => _observingCommits.Wait(1000));
        }

        protected override void Cleanup()
        {
            _subscription.Dispose();
            _observeCommits.Dispose();
        }

        [Fact]
        public void should_observe_exception_from_start_task()
        {
            _exception.InnerException.ShouldBe(_subscriberException);
        }

        [Fact]
        public void should_observe_exception_on_subscription()
        {
            _onErrorException.ShouldBe(_subscriberException);
        }
    }
}