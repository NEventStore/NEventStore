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
		private Uri url;
		private string defaultDatabase;

		public RavenPersistenceWireup(Wireup inner)
			: this(inner, string.Empty)
		{
		}

		public RavenPersistenceWireup(Wireup inner, string connectionName)
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
				ConnectionName = connectionName,
				DefaultDatabase = this.defaultDatabase,
				Url = this.url
			});

			this.Container.Register(c => new RavenPersistenceFactory(c.Resolve<RavenConfiguration>()).Build());
		}

		public virtual RavenPersistenceWireup DefaultDatabase(string database)
		{
			Logger.Debug("Using database named '{0}'.", database);

			this.defaultDatabase = database;
			return this;
		}
		public virtual RavenPersistenceWireup Url(string address)
		{
			Logger.Debug("Using database at '{0}'.", address);

			this.url = new Uri(address, UriKind.Absolute);
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
	}
}