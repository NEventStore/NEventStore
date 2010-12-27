namespace EventStore.Persistence.MongoPersistence
{
    using System;
    using System.Collections.Generic;

    public class MongoCommit
    {
        private const string IdFormat = "{0}.{1}";

        public string Id
        {
            get { return IdFormat.FormatWith(this.StreamId, this.CommitSequence); }
        }
        public Guid StreamId { get; set; }
        public Guid CommitId { get; set; }
        public long StreamRevision { get; set; }
        public long CommitSequence { get; set; }
        public Dictionary<string, object> Headers { get; set; }
        public List<EventMessage> Events { get; set; }
        public object Snapshot { get; set; }

        public bool Dispatched { get; set; }
    }
}