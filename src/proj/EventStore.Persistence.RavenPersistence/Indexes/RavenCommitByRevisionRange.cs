namespace EventStore.Persistence.RavenPersistence.Indexes
{
	using System.Linq;
	using Raven.Client.Indexes;

	public class RavenCommitByRevisionRange : AbstractIndexCreationTask<RavenCommit>
	{
		public RavenCommitByRevisionRange()
		{
			this.Map = commits => from c in commits
								  select new { c.StreamId, c.StartingStreamRevision, c.StreamRevision };
		}
	}
}