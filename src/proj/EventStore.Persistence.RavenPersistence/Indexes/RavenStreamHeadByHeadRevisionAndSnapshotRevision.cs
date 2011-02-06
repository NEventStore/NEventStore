namespace EventStore.Persistence.RavenPersistence.Indexes
{
	using System.Linq;
	using Raven.Client.Indexes;

	public class RavenStreamHeadByHeadRevisionAndSnapshotRevision : AbstractIndexCreationTask<RavenStreamHead>
	{
		public RavenStreamHeadByHeadRevisionAndSnapshotRevision()
		{
			Map = snapshots => from s in snapshots select new { s.HeadRevision, s.SnapshotRevision };
		}
	}
}