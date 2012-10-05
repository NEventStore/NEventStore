using System;
using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace EventStore.Persistence.RavenPersistence.Indexes
{
    public class EventStoreDocumentsByEntityName : AbstractIndexCreationTask
    {
        public override string IndexName
        {
            get { return "EventStoreDocumentsByEntityName"; }
        }

        public override IndexDefinition CreateIndexDefinition()
        {
            return new IndexDefinition
            {
                //Redundant ?? null needed for compatibility with older models. Please do not remove.
                Map = @"from doc in docs 
                        let Tag = doc[""@metadata""][""Raven-Entity-Name""]
                        where  Tag != null 
                        select new { Tag, LastModified = (DateTime)doc[""@metadata""][""Last-Modified""], Partition = doc.Partition ?? null };",
                Indexes =
					{
						{"Tag", FieldIndexing.NotAnalyzed},
                        {"Partition", FieldIndexing.NotAnalyzed},
					},
                Stores =
					{
						{"Tag", FieldStorage.No},
						{"LastModified", FieldStorage.No},
                        {"Partition", FieldStorage.No}
					}
            };
        }
    }
}