namespace NEventStore.Persistence.RavenPersistence.Indexes
{
    using System.Linq;
    using Raven.Client.Indexes;

    public class RavenStreamHeadBySnapshotAge : AbstractIndexCreationTask<RavenStreamHead>
    {
        public RavenStreamHeadBySnapshotAge()
        {
            //Redundant ?? null needed for compatibility with older models. Please do not remove.
            Map =
                snapshots =>
                    from s in snapshots select new { s.BucketId, SnapshotAge = s.HeadRevision - s.SnapshotRevision, Partition = s.Partition ?? null};
        }
    }
}