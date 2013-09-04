namespace NEventStore.Persistence.RavenDB.Indexes
{
    using System.Linq;
    using NEventStore.Persistence.RavenDB;
    using Raven.Client.Indexes;

    public class RavenCommitsByDispatched : AbstractIndexCreationTask<RavenCommit>
    {
        public RavenCommitsByDispatched()
        {
            Map = commits => from c in commits select new {c.Dispatched };
        }
    }
}