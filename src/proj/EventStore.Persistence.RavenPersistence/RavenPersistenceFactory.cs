namespace EventStore.Persistence.RavenPersistence
{
	using Raven.Client;
	using Raven.Client.Document;

	public class RavenPersistenceFactory : IPersistenceFactory
	{
		private readonly RavenConfiguration config;


		public RavenPersistenceFactory(RavenConfiguration config)
		{
			this.config = config;
		}

		public virtual IPersistStreams Build()
		{
			var store = this.GetStore();
			return new RavenPersistenceEngine(store, this.config);
		}
		protected virtual IDocumentStore GetStore()
		{
		    var store = new DocumentStore();

            if(!string.IsNullOrEmpty(config.ConnectionName))
                store.ConnectionStringName = config.ConnectionName;

            if (!string.IsNullOrEmpty(config.Url))
                store.Url = config.Url;
			

            store.Initialize();

			return store;
		}
	}
}