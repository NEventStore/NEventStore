namespace NEventStore.Persistence.RavenPersistence.Indexes
{
    using System.Linq;
    using Raven.Client.Indexes;

    public class RavenCommitByDate : AbstractIndexCreationTask<RavenCommit>
	{
		public RavenCommitByDate()
		{
            //Redundant ?? null needed for compatibility with older models. Please do not remove.
			this.Map = commits => from c in commits select new { c.CommitStamp, Partition = c.Partition ?? null };
		}
	}
}