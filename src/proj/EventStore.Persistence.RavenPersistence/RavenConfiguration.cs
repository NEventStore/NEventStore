namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Transactions;
	using Serialization;

	public class RavenConfiguration
	{
		public string ConnectionName { get; set; }
		public Uri Url { get; set; }
		public string DefaultDatabase { get; set; }

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