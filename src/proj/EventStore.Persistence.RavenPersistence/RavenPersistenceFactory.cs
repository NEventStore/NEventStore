namespace EventStore.Persistence.RavenPersistence
{
	using Raven.Client;
	using Raven.Client.Document;

	public class RavenPersistenceFactory : IPersistenceFactory
	{
		private readonly RavenConfiguration config;

		protected string ConnectionStringName { get; private set; }

		public RavenPersistenceFactory(string connectionName, RavenConfiguration config)
		{
			this.ConnectionStringName = connectionName;
			this.config = config;
		}

		public virtual IPersistStreams Build()
		{
			var store = this.GetStore();
			return new RavenPersistenceEngine(store, this.config);
		}
		protected virtual IDocumentStore GetStore()
		{
			var store = new DocumentStore { ConnectionStringName = this.ConnectionStringName };
			store.Initialize();

			return store;
		}
	}
}