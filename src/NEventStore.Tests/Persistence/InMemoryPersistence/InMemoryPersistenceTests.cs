#pragma warning disable 169

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
        private readonly DateTime _endDate = new DateTime(2013, 1, 2);
        private readonly DateTime _startDate = new DateTime(2013, 1, 1);
        private ICommit[] _commits;
        private InMemoryPersistenceEngine engine;

        protected override void Context()
        {
            engine = new InMemoryPersistenceEngine();
            engine.Initialize();
            var streamId = Guid.NewGuid().ToString();
            engine.Commit(new Commit(streamId, 0, Guid.NewGuid(), 0, _startDate, new Dictionary<string, object>(), new List<EventMessage>()));
            engine.Commit(new Commit(streamId, 1, Guid.NewGuid(), 1, _endDate, new Dictionary<string, object>(), new List<EventMessage>()));
        }

        protected override void Because()
        {
            _commits = engine.GetFromTo(_startDate, _endDate).ToArray();
        }

        [Fact]
        public void should_return_two_commits()
        {
            _commits.Length.ShouldBe(1);
        }
    }
}

#pragma warning restore 169