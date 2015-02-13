namespace NEventStore.CommonDomain.Persistence.EventStore
{
	using System;

	using global::CommonDomain;
	using global::CommonDomain.Persistence;
	using global::CommonDomain.Persistence.EventStore;

	using NEventStore.Persistence.AcceptanceTests.BDD;

	using Xunit;
	using Xunit.Should;

	public class using_a_sagaeventstorerepository : SpecificationBase
	{
		protected ISagaRepository _repository;

		protected IStoreEvents _storeEvents;

		protected override void Context()
		{
			this._storeEvents = Wireup.Init().UsingInMemoryPersistence().Build();
			this._repository = new SagaEventStoreRepository(this._storeEvents, new SagaFactory());
		}
	}

	public class when_an_aggregate_is_loaded : using_a_sagaeventstorerepository
	{
		private TestSaga _testSaga;

		private string _id;

		protected override void Context()
		{
			base.Context();
			_id = "something";
			_testSaga = new TestSaga(_id);
		}

		protected override void Because()
		{
			_repository.Save(_testSaga, Guid.NewGuid(), null);
		}

		[Fact]
		public void should_be_returned_when_loaded_by_id()
		{
			_repository.GetById<TestSaga>(_id).Id.ShouldBe(_testSaga.Id);
		}
	}
}
