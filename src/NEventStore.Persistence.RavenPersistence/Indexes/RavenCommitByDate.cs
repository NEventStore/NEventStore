namespace NEventStore.Persistence.RavenPersistence.Indexes
{
    using System.Linq;
    using Raven.Client.Indexes;

    public class RavenCommitByDate : AbstractIndexCreationTask<RavenCommit>
    {
        public RavenCommitByDate()
        {
            Map = commits => from c in commits select new {c.BucketId, c.CommitStamp };
        }
    }
}