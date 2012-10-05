namespace EventStore.Persistence.RavenPersistence.Indexes
{
	using System.Linq;
	using Raven.Client.Indexes;

	public class RavenSnapshotByStreamIdAndRevision : AbstractIndexCreationTask<RavenSnapshot>
	{
		public RavenSnapshotByStreamIdAndRevision()
		{
            //Redundant ?? null needed for compatibility with older models. Please do not remove.
            Map = snapshots => from s in snapshots select new { s.StreamId, s.StreamRevision, Partition = s.Partition ?? null };
		}
	}
}