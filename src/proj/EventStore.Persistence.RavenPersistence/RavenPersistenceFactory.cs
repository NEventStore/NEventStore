namespace EventStore.Persistence.RavenPersistence
{
	using System.Transactions;
	using Raven.Client;
	using Raven.Client.Document;
	using Serialization;

	public class RavenPersistenceFactory : IPersistenceFactory
	{
		private const int DefaultPageSize = 128;
		private readonly IDocumentSerializer serializer;
		private readonly TransactionScopeOption scopeOption;
		private readonly bool consistentQueries;
		private readonly int pageSize;

		protected string ConnectionStringName { get; private set; }

		public RavenPersistenceFactory(string connectionName, IDocumentSerializer serializer)
			: this(connectionName, serializer, TransactionScopeOption.Suppress, false, DefaultPageSize)
		{
		}
		public RavenPersistenceFactory(
			string connectionName,
			IDocumentSerializer serializer,
			TransactionScopeOption scopeOption,
			bool consistentQueries,
			int pageSize)
		{
			this.ConnectionStringName = connectionName;
			this.serializer = serializer;
			this.scopeOption = scopeOption;
			this.consistentQueries = consistentQueries;
			this.pageSize = pageSize;
		}

		public virtual IPersistStreams Build()
		{
			var store = this.GetStore();
			return new RavenPersistenceEngine(store, this.serializer, this.scopeOption, this.consistentQueries, this.pageSize);
		}
		protected virtual IDocumentStore GetStore()
		{
			var store = new DocumentStore { ConnectionStringName = this.ConnectionStringName };
			store.Initialize();

			return store;
		}
	}
}