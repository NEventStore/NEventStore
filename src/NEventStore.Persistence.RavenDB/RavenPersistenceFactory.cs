namespace NEventStore.Persistence.RavenDB
{
    using Raven.Client;
    using Raven.Client.Document;

    public class RavenPersistenceFactory : IPersistenceFactory
    {
        private readonly RavenConfiguration _config;

        public RavenPersistenceFactory(RavenConfiguration config)
        {
            _config = config;
        }

        public virtual IPersistStreams Build()
        {
            IDocumentStore store = GetStore();
            return new RavenPersistenceEngine(store, _config);
        }

#pragma warning disable 612,618
        protected virtual IDocumentStore GetStore()
        {
            var store = new DocumentStore();

            if (!string.IsNullOrEmpty(_config.ConnectionName))
            {
                store.ConnectionStringName = _config.ConnectionName;
            }

            if (_config.Url != null)
            {
                store.Url = _config.Url.ToString();
            }

            if (!string.IsNullOrEmpty(_config.DefaultDatabase))
            {
                store.DefaultDatabase = _config.DefaultDatabase;
            }

            store.Initialize();

            return store;
        }
#pragma warning restore 612,618
    }
}