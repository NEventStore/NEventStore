namespace EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using NEventStore;
    using NEventStore.Logging;
    using NEventStore.Persistence;
    using NEventStore.Persistence.RavenPersistence;
    using NEventStore.Serialization;
    using Raven.Client;
    using Raven.Client.Document;

    public class RavenPersistenceWireup : PersistenceWireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (RavenPersistenceWireup));
        private readonly List<Action<DocumentStore>> _customizeStoreActions = new List<Action<DocumentStore>>();
        private readonly Func<IDocumentStore> _getStoreAction;

        // these values are considered "safe by default" according to Raven docs
        private bool _consistentQueries; // stale queries perform better
        private int _maxServerPageSize = 1024;
        private int _pageSize = 128;
        private string _partition;
        private IDocumentSerializer _serializer = new DocumentObjectSerializer();

        public RavenPersistenceWireup(Wireup inner) : base(inner)
        {
            Logger.Debug("Configuring Raven persistence engine.");

            Container.Register(
                               c =>
                                   new RavenConfiguration
                                   {
                                       Serializer = ResolveSerializer(c),
                                       ScopeOption = c.Resolve<TransactionScopeOption>(),
                                       ConsistentQueries = _consistentQueries,
                                       MaxServerPageSize = _maxServerPageSize,
                                       RequestedPageSize = _pageSize,
                                       Partition = _partition,
                                   });

            Container.Register<IPersistStreams>(c => new RavenPersistenceEngine(_getStoreAction(), c.Resolve<RavenConfiguration>()));
        }

        public RavenPersistenceWireup(Wireup inner, string connectionName) : this(inner)
        {
            _getStoreAction = CreateStore;
            CreateWithConnectionStringName(connectionName);
        }

        public RavenPersistenceWireup(Wireup inner, Func<IDocumentStore> getStore) : this(inner)
        {
            _getStoreAction = getStore;
        }

        public virtual RavenPersistenceWireup ConnectionStringName(string connectionStringName)
        {
            CreateWithConnectionStringName(connectionStringName);
            return this;
        }

        public virtual RavenPersistenceWireup ConnectionString(string connectionStringValue)
        {
            Logger.Debug("Using connection string value '{0}'.", connectionStringValue);

            _customizeStoreActions.Add(s => s.ParseConnectionString(connectionStringValue));

            return this;
        }

        public virtual RavenPersistenceWireup DefaultDatabase(string database)
        {
            Logger.Debug("Using database named '{0}'.", database);

            _customizeStoreActions.Add(s => s.DefaultDatabase = database);

            return this;
        }

        public virtual RavenPersistenceWireup Url(string address)
        {
            Logger.Debug("Using database at '{0}'.", address);

            _customizeStoreActions.Add(s => s.Url = address);

            return this;
        }

        public virtual RavenPersistenceWireup Partition(string name)
        {
            _partition = name;

            return this;
        }

        public virtual RavenPersistenceWireup PageEvery(int records)
        {
            Logger.Debug("Page result set every {0} records.", records);

            _pageSize = records;
            return this;
        }

        public virtual RavenPersistenceWireup MaxServerPageSizeConfiguration(int records)
        {
            Logger.Debug("The maximum allowed page size as configured on the Raven server is {0} records.", records);

            _maxServerPageSize = records;
            return this;
        }

        public virtual RavenPersistenceWireup ConsistentQueries(bool fullyConsistent)
        {
            Logger.Debug("Queries to Raven will return results which are fully consistent: {0}", fullyConsistent);

            _consistentQueries = fullyConsistent;
            return this;
        }

        public virtual RavenPersistenceWireup ConsistentQueries()
        {
            return ConsistentQueries(true);
        }

        public virtual RavenPersistenceWireup StaleQueries()
        {
            return ConsistentQueries(false);
        }

        public virtual RavenPersistenceWireup CustomizeDocumentStore(Action<IDocumentStore> customizationAction)
        {
            _customizeStoreActions.Add(customizationAction);

            return this;
        }

        public virtual RavenPersistenceWireup WithSerializer(IDocumentSerializer instance)
        {
            Logger.Debug("Registering serializer of type '{0}'.", instance.GetType());

            _serializer = instance;
            return this;
        }

        private IDocumentSerializer ResolveSerializer(NanoContainer container)
        {
            var registered = container.Resolve<ISerialize>();
            if (registered == null)
            {
                return _serializer;
            }

            Logger.Debug("Wrapping registered serializer of type '{0}' inside of a ByteStreamDocumentSerializer", registered.GetType());
            return new ByteStreamDocumentSerializer(registered);
        }

        private void CreateWithConnectionStringName(string connectionStringName)
        {
            Logger.Debug("Using connection string named '{0}'.", connectionStringName);

            _customizeStoreActions.Add(s => s.ConnectionStringName = connectionStringName);
        }

        private IDocumentStore CreateStore()
        {
            var store = new DocumentStore();

            foreach (var action in _customizeStoreActions)
            {
                action(store);
            }

            return store.Initialize();
        }
    }
}