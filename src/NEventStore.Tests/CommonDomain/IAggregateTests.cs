namespace CommonDomain
{
    using System;
    using FluentAssertions;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;

    public class when_an_aggregate_is_created : SpecificationBase
    {
        private TestAggregate _testAggregate;

        protected override void Because()
        {
            _testAggregate = new TestAggregate(Guid.NewGuid(), "Test");
        }

        [Fact]
        public void should_have_name()
        {
            _testAggregate.Name.Should().Be("Test");
        }

        [Fact]
        public void aggregate_version_should_be_one()
        {
            _testAggregate.Version.Should().Be(1);
        }
    }

    public class when_updating_an_aggregate : SpecificationBase
    {
        private TestAggregate _testAggregate;

        protected override void Context()
        {
            _testAggregate = new TestAggregate(Guid.NewGuid(), "Test");
        }

        protected override void Because()
        {
            _testAggregate.ChangeName("UpdatedTest");
        }

        [Fact]
        public void name_change_should_be_applied()
        {
            _testAggregate.Name.Should().Be("UpdatedTest");
        }

        [Fact]
        public void applying_events_automatically_increments_version()
        {
            _testAggregate.Version.Should().Be(2);
        }
    }
}