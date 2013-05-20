using EventStore.Persistence.AcceptanceTests;
using Xunit;
using System;
using System.Linq;
using Xunit.Should;

namespace EventStore.Persistence.SqlPersistence.Tests.Bugs
{
    public class Issue159OrderingByCommitStampIsNotReliable<TFactory> : PersistenceEngineConcern
    {
        static Commit[] undispatched;

        protected override void Context()
        {
            var streamId = Guid.NewGuid();

            Persistence.Purge();
            var dateTime = new DateTime(2013, 1, 1);
            SystemTime.Resolver = () => dateTime;
            // deliberately out of order
            Persistence.Commit(new Commit(streamId, 0, Guid.NewGuid(), 0, SystemTime.UtcNow, null, null));
            Persistence.Commit(new Commit(streamId, 2, Guid.NewGuid(), 2, SystemTime.UtcNow, null, null));
            Persistence.Commit(new Commit(streamId, 1, Guid.NewGuid(), 1, SystemTime.UtcNow, null, null));
        }

        protected override void Because()
        {
            undispatched = Persistence.GetUndispatchedCommits().ToArray();
        }

        [Fact]
        public void should_have_commits_in_correct_order()
        {
            for (var i = 0; i < undispatched.Length; i++)
            {
                undispatched[i].CommitSequence.ShouldBe(i);
            }
        }

    }
}