namespace EventStore
{
	using System.Transactions;
	using Persistence.RavenPersistence;
	using Serialization;

	public class RavenPersistenceWireup : PersistenceWireup
	{
		// these values are considered "safe by default" according to Raven docs
		private int pageSize = 128;
		private int maxServerPageSize = 1024;
		private bool consistentQueries; // stale queries perform better
		private IDocumentSerializer serializer = new DocumentObjectSerializer();
		private string url;
		private string defaultDatabase;

		public RavenPersistenceWireup(Wireup inner)
			: this(inner, string.Empty)
		{
		}

		public RavenPersistenceWireup(Wireup inner, string connectionName)
			: base(inner)
		{
			this.Container.Register(c => new RavenConfiguration
			{
				Serializer = this.ResolveSerializer(c),
				ScopeOption = c.Resolve<TransactionScopeOption>(),
				ConsistentQueries = this.consistentQueries,
				MaxServerPageSize = this.maxServerPageSize,
				RequestedPageSize = this.pageSize,
				ConnectionName = connectionName,
				DefaultDatabase = this.defaultDatabase,
				Url = this.url
			});

			this.Container.Register(c => new RavenPersistenceFactory(c.Resolve<RavenConfiguration>()).Build());
		}

		public virtual RavenPersistenceWireup DefaultDatabase(string database)
		{
			this.defaultDatabase = database;
			return this;
		}
		public virtual RavenPersistenceWireup Url(string explicitUrl)
		{
			this.url = explicitUrl;
			return this;
		}

		public virtual RavenPersistenceWireup PageEvery(int records)
		{
			this.pageSize = records;
			return this;
		}
		public virtual RavenPersistenceWireup MaxServerPageSizeConfiguration(int records)
		{
			this.maxServerPageSize = records;
			return this;
		}

		public virtual RavenPersistenceWireup ConsistentQueries(bool fullyConsistent)
		{
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

		public virtual RavenPersistenceWireup WithSerializer(IDocumentSerializer instance)
		{
			this.serializer = instance;
			return this;
		}
		private IDocumentSerializer ResolveSerializer(NanoContainer container)
		{
			var registered = container.Resolve<ISerialize>();
			if (registered == null)
				return this.serializer;

			return new ByteStreamDocumentSerializer(registered);
		}
	}
}