namespace EventStore.Persistence.RavenPersistence.Indexes
{
	using System.Linq;
	using Raven.Client.Indexes;

	public class RavenSnapshotByStreamIdAndRevision : AbstractIndexCreationTask<RavenSnapshot>
	{
		public RavenSnapshotByStreamIdAndRevision()
		{
            Map = snapshots => from s in snapshots select new { s.StreamId, s.StreamRevision, s.Partition };
		}
	}
}