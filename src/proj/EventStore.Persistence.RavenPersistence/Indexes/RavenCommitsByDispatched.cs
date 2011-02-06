namespace EventStore.Persistence.RavenPersistence.Indexes
{
	using System.Linq;
	using Raven.Client.Indexes;

	public class RavenCommitsByDispatched : AbstractIndexCreationTask<RavenCommit>
	{
		public RavenCommitsByDispatched()
		{
			this.Map = commits => from c in commits select new { c.Dispatched };
		}
	}
}