using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace EventStore.Persistence.AzureTablesPersistence.Datastructures
{
    public class AzureCommit : TableEntity
    {
        public int StreamRevision { get; set; }
        public Guid CommitId { get; set; }
        public DateTime CommitStamp { get; set; }
        public byte[] Headers { get; set; }
        public byte[] Payload { get; set; }
        public bool Dispatched { get; set; }
    }
}