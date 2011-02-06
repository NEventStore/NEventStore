namespace EventStore.Persistence.RavenPersistence.Indexes
{
	using System.Linq;
	using Raven.Client.Indexes;

	public class RavenCommitByDate : AbstractIndexCreationTask<RavenCommit>
	{
		public RavenCommitByDate()
		{
			this.Map = commits => from c in commits select new { c.CommitStamp };
		}
	}
}