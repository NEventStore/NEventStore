namespace EventStore.Core.UnitTests.Persistence.InMemoryPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EventStore.Persistence.InMemoryPersistence;
    using Machine.Specifications;

    [Subject("InMemoryPersistence")]
    public class when_getting_from_to_then_should_not_get_later_commits
    {
        static InMemoryPersistenceEngine engine;
        static Commit[] commits;
        static DateTime startDate = new DateTime(2013, 1, 1);
        static DateTime endDate = new DateTime(2013, 1, 2);

        Establish context = () =>
            {
                engine = new InMemoryPersistenceEngine();
                engine.Initialize();
                Guid streamId = Guid.NewGuid();
                engine.Commit(new Commit(streamId, 0, Guid.NewGuid(), 0, startDate, new Dictionary<string, object>(), new List<EventMessage>()));
                engine.Commit(new Commit(streamId, 1, Guid.NewGuid(), 1, endDate, new Dictionary<string, object>(), new List<EventMessage>()));
                engine.Commit(new Commit(streamId, 2, Guid.NewGuid(), 2, endDate.AddDays(1), new Dictionary<string, object>(), new List<EventMessage>()));
            };

        Because of = () => commits = engine.GetFromTo(startDate, endDate).ToArray();

        It should_return_two_commits = () => commits.Length.ShouldEqual(2);
    }
}