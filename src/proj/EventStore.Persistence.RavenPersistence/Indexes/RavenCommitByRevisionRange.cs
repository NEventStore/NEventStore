using System.Linq;
using Raven.Client.Indexes;

namespace EventStore.Persistence.RavenPersistence.Indexes
{
    public class RavenCommitByRevisionRange : AbstractIndexCreationTask<RavenCommit>
    {
        public RavenCommitByRevisionRange()
        {
            Map = commits => from c in commits select new { c.StartingStreamRevision, c.StreamRevision };
        }
    }
}