namespace NEventStore.Persistence.RavenDB.Indexes
{
    using System.Linq;
    using NEventStore.Persistence.RavenDB;
    using Raven.Client.Indexes;

    public class RavenCommitByDate : AbstractIndexCreationTask<RavenCommit>
    {
        public RavenCommitByDate()
        {
            Map = commits => from c in commits select new {c.BucketId, c.CommitStamp };
        }
    }
}