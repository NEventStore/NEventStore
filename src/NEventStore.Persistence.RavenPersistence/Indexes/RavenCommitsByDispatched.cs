namespace NEventStore.Persistence.RavenPersistence.Indexes
{
    using System.Linq;
    using Raven.Client.Indexes;

    public class RavenCommitsByDispatched : AbstractIndexCreationTask<RavenCommit>
    {
        public RavenCommitsByDispatched()
        {
            Map = commits => from c in commits select new {c.Dispatched };
        }
    }
}