using System.Linq;
using Raven.Client.Indexes;

namespace EventStore.Persistence.RavenPersistence.Indexes
{
    public class RavenSnapshotByStreamIdAndRevision : AbstractIndexCreationTask<RavenSnapshot>
    {
        public RavenSnapshotByStreamIdAndRevision()
        {
            Map = snapshots => from s in snapshots select new { s.StreamId, s.StreamRevision };
        }
    }
}