namespace NEventStore.Persistence.RavenPersistence.Indexes
{
    using System.Linq;
    using Raven.Client.Indexes;

    public class RavenCommitByDate : AbstractIndexCreationTask<RavenCommit>
    {
        public RavenCommitByDate()
        {
            //Redundant ?? null needed for compatibility with older models. Please do not remove.
            Map = commits => from c in commits select new {c.BucketId, c.CommitStamp, Partition = c.Partition ?? null};
        }
    }
}