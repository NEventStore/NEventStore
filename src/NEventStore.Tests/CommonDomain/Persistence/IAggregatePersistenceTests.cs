namespace CommonDomain
{
	using System;

	using NEventStore;
	using NEventStore.Persistence.AcceptanceTests.BDD;

	using Xunit;
	using Xunit.Should;

	using global::CommonDomain.Core;
	using global::CommonDomain.Persistence;
	using global::CommonDomain.Persistence.EventStore;
	
	public class using_a_configured_repository : SpecificationBase
	{
		protected IRepository _repository;

		protected IStoreEvents _storeEvents;

		protected override void Context()
		{
			this._storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
			this._repository = new EventStoreRepository(this._storeEvents, new AggregateFactory(), new ConflictDetector());
		}
	}

	public class when_an_aggregate_is_persisted : using_a_configured_repository
	{
		private TestAggregate _testAggregate;

		private Guid _id;

		protected override void Context()
		{
			base.Context();
			_id = Guid.NewGuid();
			_testAggregate = new TestAggregate(_id, "Test");
		}

		protected override void Because()
		{
			_repository.Save(_testAggregate, Guid.NewGuid(), null);
		}

		[Fact]
		public void should_be_returned_when_loaded_by_id()
		{
			_repository.GetById<TestAggregate>(_id).Name.ShouldBe(_testAggregate.Name);
		}
	}

	public class when_a_persisted_aggregate_is_updated : using_a_configured_repository
	{
		private Guid _id;

		private const string NewName = "UpdatedName";

		protected override void Context()
		{
			base.Context();
			_id = Guid.NewGuid();
			_repository.Save(new TestAggregate(_id, "Test"), Guid.NewGuid(), null);
		}

		protected override void Because()
		{
			var aggregate = _repository.GetById<TestAggregate>(_id);
			aggregate.ChangeName(NewName);
			_repository.Save(aggregate,Guid.NewGuid(), null);
		}

		[Fact]
		public void should_have_updated_name()
		{
			_repository.GetById<TestAggregate>(_id).Name.ShouldBe(NewName);
		}

		[Fact]
		public void should_have_updated_version()
		{
			_repository.GetById<TestAggregate>(_id).Version.ShouldBe(2);
		}
	}

	public class when_a_loading_a_specific_aggregate_version : using_a_configured_repository
	{
		private Guid _id;

		private const string VersionOneName = "Test";
		private const string NewName = "UpdatedName";

		protected override void Context()
		{
			base.Context();
			_id = Guid.NewGuid();
			_repository.Save(new TestAggregate(_id, VersionOneName), Guid.NewGuid(), null);
		}

		protected override void Because()
		{
			var aggregate = _repository.GetById<TestAggregate>(_id);
			aggregate.ChangeName(NewName);
			_repository.Save(aggregate, Guid.NewGuid(), null);
			_repository.Dispose();
		}

		[Fact]
		public void should_be_able_to_load_initial_version()
		{
			_repository.GetById<TestAggregate>(_id, 1).Name.ShouldBe(VersionOneName);
		}
	}

	public class when_an_aggregate_is_persisted_to_specific_bucket : using_a_configured_repository
	{
		private TestAggregate _testAggregate;

		private Guid _id;

		private string _bucket;

		protected override void Context()
		{
			base.Context();
			_id = Guid.NewGuid();
			_bucket = "TenantB";
			_testAggregate = new TestAggregate(_id, "Test");
		}

		protected override void Because()
		{
			_repository.Save(_bucket, _testAggregate, Guid.NewGuid(), null);
		}

		[Fact]
		public void should_be_returned_when_loaded_by_id()
		{
			_repository.GetById<TestAggregate>(_bucket, _id).Name.ShouldBe(_testAggregate.Name);
		}
	}
}