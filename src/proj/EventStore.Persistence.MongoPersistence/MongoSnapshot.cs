namespace EventStore.Persistence.MongoPersistence
{
	using System;

	public class MongoSnapshot
	{
		private const string IdFormat = "{0}.{1}";

		public string Id
		{
			get { return IdFormat.FormatWith(this.StreamId, this.StreamRevision); }
		}

		public Guid StreamId { get; set; }
		public int StreamRevision { get; set; }
		public byte[] Payload { get; set; }
	}
}