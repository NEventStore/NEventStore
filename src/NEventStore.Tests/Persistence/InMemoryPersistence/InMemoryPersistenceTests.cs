
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore.Persistence.InMemoryPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class when_getting_from_to_then_should_not_get_later_commits : SpecificationBase
    {
        private readonly DateTime endDate = new DateTime(2013, 1, 2);
        private readonly DateTime startDate = new DateTime(2013, 1, 1);
        private Commit[] commits;
        private InMemoryPersistenceEngine engine;

        protected override void Context()
        {
            engine = new InMemoryPersistenceEngine();
            engine.Initialize();
            string streamId = Guid.NewGuid().ToString();
            engine.Commit(new Commit(streamId, 0, Guid.NewGuid(), 0, startDate, new Dictionary<string, object>(), new List<EventMessage>()));
            engine.Commit(new Commit(streamId, 1, Guid.NewGuid(), 1, endDate, new Dictionary<string, object>(), new List<EventMessage>()));
        }

        protected override void Because()
        {
            commits = engine.GetFromTo(startDate, endDate).ToArray();
        }

        [Fact]
        public void should_return_two_commits()
        {
            commits.Length.ShouldBe(1);
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169