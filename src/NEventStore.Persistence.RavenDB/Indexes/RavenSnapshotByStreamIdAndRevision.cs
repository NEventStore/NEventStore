namespace NEventStore.Persistence.RavenDB.Indexes
{
    using System.Linq;
    using NEventStore.Persistence.RavenDB;
    using Raven.Client.Indexes;

    public class RavenSnapshotByStreamIdAndRevision : AbstractIndexCreationTask<RavenSnapshot>
    {
        public RavenSnapshotByStreamIdAndRevision()
        {
            Map = snapshots => from s in snapshots select new { s.BucketId, s.StreamId, s.StreamRevision };
        }
    }
}