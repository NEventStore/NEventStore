using System.Collections.Generic;
using EventStore.Persistence;
using Raven.Client;
using Raven.Client.Document;

namespace EventStore
{
	using System;
	using System.Transactions;
	using Logging;
	using Persistence.RavenPersistence;
	using Serialization;

	public class RavenPersistenceWireup : PersistenceWireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(RavenPersistenceWireup));

		// these values are considered "safe by default" according to Raven docs
		private int pageSize = 128;
		private int maxServerPageSize = 1024;
		private bool consistentQueries; // stale queries perform better
		private IDocumentSerializer serializer = new DocumentObjectSerializer();
	    private string partition = null;

	    Func<IDocumentStore> getStoreAction;
	    readonly List<Action<DocumentStore>> customizeStoreActions = new List<Action<DocumentStore>>(); 

	    public RavenPersistenceWireup(Wireup inner)
			: base(inner)
		{
            Logger.Debug("Configuring Raven persistence engine.");

            this.Container.Register(c => new RavenConfiguration
            {
                Serializer = this.ResolveSerializer(c),
                ScopeOption = c.Resolve<TransactionScopeOption>(),
                ConsistentQueries = this.consistentQueries,
                MaxServerPageSize = this.maxServerPageSize,
                RequestedPageSize = this.pageSize,
                Partition = this.partition,
            });

            this.Container.Register<IPersistStreams>(c => new RavenPersistenceEngine(getStoreAction(), c.Resolve<RavenConfiguration>()));
		}

		public RavenPersistenceWireup(Wireup inner, string connectionName)
            : this(inner)
		{
		    getStoreAction = CreateStore;
		    CreateWithConnectionStringName(connectionName);
		}

        public RavenPersistenceWireup(Wireup inner, Func<IDocumentStore> getStore)
            : this(inner)
        {
            getStoreAction = getStore;
        }

        public virtual RavenPersistenceWireup ConnectionStringName(string connectionStringName)
        {
            CreateWithConnectionStringName(connectionStringName);
            return this;
        }
        
	    public virtual RavenPersistenceWireup ConnectionString(string connectionStringValue)
        {
            Logger.Debug("Using connection string value '{0}'.", connectionStringValue);

            customizeStoreActions.Add(s => s.ParseConnectionString(connectionStringValue));
            
            return this;
        }

		public virtual RavenPersistenceWireup DefaultDatabase(string database)
		{
			Logger.Debug("Using database named '{0}'.", database);

			customizeStoreActions.Add(s => s.DefaultDatabase = database);

			return this;
		}

		public virtual RavenPersistenceWireup Url(string address)
		{
			Logger.Debug("Using database at '{0}'.", address);

            customizeStoreActions.Add(s => s.Url = address);

			return this;
		}

        public virtual RavenPersistenceWireup Partition(string name)
        {
            this.partition = name;

            return this;
        }

		public virtual RavenPersistenceWireup PageEvery(int records)
		{
			Logger.Debug("Page result set every {0} records.", records);

			this.pageSize = records;
			return this;
		}

		public virtual RavenPersistenceWireup MaxServerPageSizeConfiguration(int records)
		{
			Logger.Debug("The maximum allowed page size as configured on the Raven server is {0} records.", records);

			this.maxServerPageSize = records;
			return this;
		}

		public virtual RavenPersistenceWireup ConsistentQueries(bool fullyConsistent)
		{
			Logger.Debug("Queries to Raven will return results which are fully consistent: {0}", fullyConsistent);

			this.consistentQueries = fullyConsistent;
			return this;
		}

		public virtual RavenPersistenceWireup ConsistentQueries()
		{
			return this.ConsistentQueries(true);
		}

		public virtual RavenPersistenceWireup StaleQueries()
		{
			return this.ConsistentQueries(false);
		}

        public virtual RavenPersistenceWireup CustomizeDocumentStore(Action<IDocumentStore> customizationAction)
        {
            customizeStoreActions.Add(customizationAction);

            return this;
        }

		public virtual RavenPersistenceWireup WithSerializer(IDocumentSerializer instance)
		{
			Logger.Debug("Registering serializer of type '{0}'.", instance.GetType());

			this.serializer = instance;
			return this;
		}
        
		private IDocumentSerializer ResolveSerializer(NanoContainer container)
		{
			var registered = container.Resolve<ISerialize>();
			if (registered == null)
				return this.serializer;

			Logger.Debug("Wrapping registered serializer of type '{0}' inside of a ByteStreamDocumentSerializer", registered.GetType());
			return new ByteStreamDocumentSerializer(registered);
		}

        private void CreateWithConnectionStringName(string connectionStringName)
        {
            Logger.Debug("Using connection string named '{0}'.", connectionStringName);

            customizeStoreActions.Add(s => s.ConnectionStringName = connectionStringName);
        }

        private IDocumentStore CreateStore()
        {
            var store = new DocumentStore();
            
            foreach (var action in customizeStoreActions)
            {
                action(store);
            }

            return store.Initialize();
        }
	}
}