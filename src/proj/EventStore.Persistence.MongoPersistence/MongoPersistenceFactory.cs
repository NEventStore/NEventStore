namespace EventStore.Persistence.MongoPersistence
{
	using System.Configuration;
	using MongoDB.Driver;
	using NEventStore.Persistence;
	using NEventStore.Serialization;

    public class MongoPersistenceFactory : IPersistenceFactory
	{
		private readonly string connectionName;
		private readonly IDocumentSerializer serializer;

		public MongoPersistenceFactory(string connectionName, IDocumentSerializer serializer)
		{
			this.connectionName = connectionName;
			this.serializer = serializer;
		}

		public virtual IPersistStreams Build()
		{
			var connectionString = this.TransformConnectionString(this.GetConnectionString());
            var builder = new MongoUrlBuilder(connectionString);
		    var database = (new MongoClient(connectionString)).GetServer().GetDatabase(builder.DatabaseName);
			return new MongoPersistenceEngine(database, this.serializer);
		}

		protected virtual string GetConnectionString()
		{
			return ConfigurationManager.ConnectionStrings[this.connectionName].ConnectionString;
		}

		protected virtual string TransformConnectionString(string connectionString)
		{
			return connectionString;
		}
	}
}