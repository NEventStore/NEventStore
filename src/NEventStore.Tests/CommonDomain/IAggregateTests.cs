namespace CommonDomain
{
	using System;

	using NEventStore.Persistence.AcceptanceTests.BDD;

	using Xunit;
	using Xunit.Should;

	public class when_an_aggregate_is_created : SpecificationBase
	{
		private TestAggregate _testAggregate;

		protected override void Because()
		{
			this._testAggregate = new TestAggregate(Guid.NewGuid(), "Test");
		}

		[Fact]
		public void should_have_name()
		{
			this._testAggregate.Name.ShouldBe("Test");
		}

		[Fact]
		public void aggregate_version_should_be_one()
		{
			this._testAggregate.Version.ShouldBe(1);
		}
	}

	public class when_updating_an_aggregate : SpecificationBase
	{
		private TestAggregate _testAggregate;

		protected override void Context()
		{
			this._testAggregate = new TestAggregate(Guid.NewGuid(), "Test");
		}

		protected override void Because()
		{
			_testAggregate.ChangeName("UpdatedTest");
		}

		[Fact]
		public void name_change_should_be_applied()
		{
			this._testAggregate.Name.ShouldBe("UpdatedTest");
		}

		[Fact]
		public void applying_events_automatically_increments_version()
		{
			this._testAggregate.Version.ShouldBe(2);
		}
	}
}