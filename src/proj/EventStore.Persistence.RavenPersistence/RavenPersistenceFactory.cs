namespace EventStore.Persistence.RavenPersistence
{
	using Raven.Client;
	using Raven.Client.Document;
	using Serialization;

	public class RavenPersistenceFactory : IPersistenceFactory
	{
		private readonly IDocumentSerializer serializer;
		private readonly bool consistentQueries;

		protected string ConnectionStringName { get; private set; }

		public RavenPersistenceFactory(string connectionName, IDocumentSerializer serializer)
			: this(connectionName, serializer, false)
		{
		}
		public RavenPersistenceFactory(string connectionName, IDocumentSerializer serializer, bool consistentQueries)
		{
			this.ConnectionStringName = connectionName;
			this.serializer = serializer;
			this.consistentQueries = consistentQueries;
		}

		public virtual IPersistStreams Build()
		{
			var store = this.GetStore();
			return new RavenPersistenceEngine(store, this.serializer, this.consistentQueries);
		}
		protected virtual IDocumentStore GetStore()
		{
			var store = new DocumentStore { ConnectionStringName = this.ConnectionStringName };
			store.Initialize();

			return store;
		}
	}
}