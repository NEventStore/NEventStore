namespace NEventStore.Persistence.RavenPersistence.Indexes
{
    using System.Linq;
    using Raven.Client.Indexes;

    public class RavenCommitsByDispatched : AbstractIndexCreationTask<RavenCommit>
    {
        public RavenCommitsByDispatched()
        {
            //Redundant ?? null needed for compatibility with older models. Please do not remove.
            Map = commits => from c in commits select new {c.Dispatched, Partition = c.Partition ?? null};
        }
    }
}