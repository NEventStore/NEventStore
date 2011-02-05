using System.Linq;
using Raven.Client.Indexes;

namespace EventStore.Persistence.RavenPersistence.Indexes
{
    public class RavenCommitByDate : AbstractIndexCreationTask<RavenCommit>
    {
        public RavenCommitByDate()
        {
            Map = commits => from c in commits select new { c.CommitStamp };
        }
    }
}