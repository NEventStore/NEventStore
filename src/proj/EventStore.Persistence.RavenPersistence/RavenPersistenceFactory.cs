namespace EventStore.Persistence.RavenPersistence
{
	using Raven.Client;
	using Raven.Client.Document;
	using Serialization;

	public class RavenPersistenceFactory : IPersistenceFactory
	{
		private readonly ISerialize serializer;

		protected string ConnectionStringName { get; private set; }

		public RavenPersistenceFactory(string connectionName, ISerialize serializer)
		{
			this.ConnectionStringName = connectionName;
			this.serializer = serializer;
		}

		public virtual IPersistStreams Build()
		{
			var store = this.GetStore();
			return new RavenPersistenceEngine(store, this.serializer);
		}
		protected virtual IDocumentStore GetStore()
		{
			var store = new DocumentStore { ConnectionStringName = this.ConnectionStringName };
			store.Initialize();

			return store;
		}
	}
}