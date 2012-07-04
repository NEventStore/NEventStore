namespace EventStore.Persistence.MongoPersistence
{
	using System.Configuration;
	using MongoDB.Driver;
	using Serialization;

	public class MongoPersistenceFactory : IPersistenceFactory
	{
		private readonly string connection;
		private readonly IDocumentSerializer serializer;

		public MongoPersistenceFactory(string connection, IDocumentSerializer serializer)
		{
			this.connection = connection;
			this.serializer = serializer;
		}

		public virtual IPersistStreams Build()
		{
			var connectionString = this.TransformConnectionString(this.GetConnectionString());
			var database = MongoDatabase.Create(connectionString);
			return new MongoPersistenceEngine(database, this.serializer);
		}

		protected virtual string GetConnectionString()
		{
		    return connection.ToLower().StartsWith("mongodb://") ? connection :
                ConfigurationManager.ConnectionStrings[this.connection].ConnectionString;
		}

	    protected virtual string TransformConnectionString(string connectionString)
		{
			return connectionString;
		}
	}
}