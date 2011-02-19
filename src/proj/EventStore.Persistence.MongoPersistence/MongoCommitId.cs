namespace EventStore.Persistence.MongoPersistence
{
	using System;

    public class MongoCommitId
	{
		public Guid StreamId { get; private set; }
		public int CommitSequence { get; private set; }

		public MongoCommitId(Guid streamId, int commitSequence)
		{
			this.StreamId = streamId;
			this.CommitSequence = commitSequence;
		}
	}
}