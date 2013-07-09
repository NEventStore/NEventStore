namespace NEventStore.Persistence.RavenPersistence.Indexes
{
    using System.Linq;
    using Raven.Client.Indexes;

    public class RavenCommitByRevisionRange : AbstractIndexCreationTask<RavenCommit>
	{
		public RavenCommitByRevisionRange()
		{
            //Redundant ?? null needed for compatibility with older models. Please do not remove.
			this.Map = commits => from c in commits
                                  select new { c.StreamId, c.StartingStreamRevision, c.StreamRevision, Partition = c.Partition ?? null };
		}
	}
}