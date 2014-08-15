namespace CommonDomain
{
    using System;
    using CommonDomain.Core;
    using CommonDomain.Persistence;
    using CommonDomain.Persistence.EventStore;
    using FluentAssertions;
    using NEventStore;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;

    public class using_a_configured_repository : SpecificationBase
    {
        protected IRepository Repository;

        protected IStoreEvents StoreEvents;

        protected override void Context()
        {
            StoreEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            Repository = new EventStoreRepository(StoreEvents, new AggregateFactory(), new ConflictDetector());
        }
    }

    public class when_an_aggregate_is_persisted : using_a_configured_repository
    {
        private Guid _id;
        private TestAggregate _testAggregate;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _testAggregate = new TestAggregate(_id, "Test");
        }

        protected override void Because()
        {
            Repository.Save(_testAggregate, Guid.NewGuid(), null);
        }

        [Fact]
        public void should_be_returned_when_loaded_by_id()
        {
            Repository.GetById<TestAggregate>(_id).Name.Should().Be(_testAggregate.Name);
        }
    }

    public class when_a_persisted_aggregate_is_updated : using_a_configured_repository
    {
        private const string NewName = "UpdatedName";
        private Guid _id;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            Repository.Save(new TestAggregate(_id, "Test"), Guid.NewGuid(), null);
        }

        protected override void Because()
        {
            var aggregate = Repository.GetById<TestAggregate>(_id);
            aggregate.ChangeName(NewName);
            Repository.Save(aggregate, Guid.NewGuid(), null);
        }

        [Fact]
        public void should_have_updated_name()
        {
            Repository.GetById<TestAggregate>(_id).Name.Should().Be(NewName);
        }

        [Fact]
        public void should_have_updated_version()
        {
            Repository.GetById<TestAggregate>(_id).Version.Should().Be(2);
        }
    }

    public class when_a_loading_a_specific_aggregate_version : using_a_configured_repository
    {
        private const string VersionOneName = "Test";
        private const string NewName = "UpdatedName";
        private Guid _id;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            Repository.Save(new TestAggregate(_id, VersionOneName), Guid.NewGuid(), null);
        }

        protected override void Because()
        {
            var aggregate = Repository.GetById<TestAggregate>(_id);
            aggregate.ChangeName(NewName);
            Repository.Save(aggregate, Guid.NewGuid(), null);
            Repository.Dispose();
        }

        [Fact]
        public void should_be_able_to_load_initial_version()
        {
            Repository.GetById<TestAggregate>(_id, 1).Name.Should().Be(VersionOneName);
        }
    }

    public class when_an_aggregate_is_persisted_to_specific_bucket : using_a_configured_repository
    {
        private string _bucket;
        private Guid _id;
        private TestAggregate _testAggregate;

        protected override void Context()
        {
            base.Context();
            _id = Guid.NewGuid();
            _bucket = "TenantB";
            _testAggregate = new TestAggregate(_id, "Test");
        }

        protected override void Because()
        {
            Repository.Save(_bucket, _testAggregate, Guid.NewGuid(), null);
        }

        [Fact]
        public void should_be_returned_when_loaded_by_id()
        {
            Repository.GetById<TestAggregate>(_bucket, _id).Name.Should().Be(_testAggregate.Name);
        }
    }

    public class when_an_aggregate_is_persisted_concurrently_by_two_clients : SpecificationBase
    {
        private Guid _aggregateId;
        protected IRepository _repository1;
        protected IRepository _repository2;

        protected IStoreEvents _storeEvents;
        private Exception _thrown;

        protected override void Context()
        {
            base.Context();

            _storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
            _repository1 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());
            _repository2 = new EventStoreRepository(_storeEvents, new AggregateFactory(), new ConflictDetector());

            _aggregateId = Guid.NewGuid();
            var aggregate = new TestAggregate(_aggregateId, "my name is..");
            _repository1.Save(aggregate, Guid.NewGuid());
        }

        protected override void Because()
        {
            var agg1 = _repository1.GetById<TestAggregate>(_aggregateId);
            var agg2 = _repository2.GetById<TestAggregate>(_aggregateId);
            agg1.ChangeName("one");
            agg2.ChangeName("two");

            _repository1.Save(agg1, Guid.NewGuid());

            _thrown = Catch.Exception(() => _repository2.Save(agg2, Guid.NewGuid()));
        }

        [Fact]
        public void should_throw_a_ConflictingCommandException()
        {
            _thrown.Should().BeOfType<ConflictingCommandException>();
        }
    }
}