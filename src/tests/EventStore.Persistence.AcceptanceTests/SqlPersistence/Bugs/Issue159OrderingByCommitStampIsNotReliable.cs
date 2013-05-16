namespace EventStore.Persistence.AcceptanceTests.SqlPersistence.Bugs
{
    using System;
    using System.Linq;
    using Machine.Specifications;

    public class Issue159OrderingByCommitStampIsNotReliable : using_the_persistence_engine
    {
        static Commit[] undispatched;

        private Establish context = () =>
                                    {
                                        persistence.Purge();
                                        var dateTime = new DateTime(2013, 1, 1);
                                        SystemTime.Resolver = () => dateTime;
                                        // deliberately out of order
                                        persistence.Commit(new Commit(streamId, 0, Guid.NewGuid(), 0, SystemTime.UtcNow, null, null));
                                        persistence.Commit(new Commit(streamId, 2, Guid.NewGuid(), 2, SystemTime.UtcNow, null, null)); 
                                        persistence.Commit(new Commit(streamId, 1, Guid.NewGuid(), 1, SystemTime.UtcNow, null, null));
                                    };

        private Because of = () => undispatched = persistence.GetUndispatchedCommits().ToArray();

        private It should_have_commits_in_correct_order = () =>
                                                          {
                                                              for (var i = 0; i < undispatched.Length; i++)
                                                              {
                                                                  undispatched[i].CommitSequence.ShouldEqual(i);
                                                              }
                                                          };
    }
}