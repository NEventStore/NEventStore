using Raven.Client.Document;

namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Transactions;
	using Serialization;

	public class RavenConfiguration
	{
        [Obsolete("This will be removed after 3.2")]
		public string ConnectionName { get; set; }

        [Obsolete("This will be removed after 3.2")]
		public Uri Url { get; set; }

        [Obsolete("This will be removed after 3.2")]
        public string DefaultDatabase { get; set; }

        public string Partition { get; set; }

		public IDocumentSerializer Serializer { get; set; }
		public TransactionScopeOption ScopeOption { get; set; }
		public bool ConsistentQueries { get; set; }
		public int RequestedPageSize { get; set; }
		public int MaxServerPageSize { get; set; }

		public int PageSize
		{
			get
			{
				if (this.RequestedPageSize > this.MaxServerPageSize)
					return this.MaxServerPageSize;

				return this.RequestedPageSize;
			}
		}
	}
}