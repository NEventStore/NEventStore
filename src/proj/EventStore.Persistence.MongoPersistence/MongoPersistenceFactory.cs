namespace EventStore.Persistence.MongoPersistence
{
	using System.Configuration;
	using MongoDB.Bson.Serialization;
	using MongoDB.Driver;
	using Serialization;

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
			if (!BsonClassMap.IsClassMapRegistered(typeof(EventMessage)))
			{
				BsonClassMap.RegisterClassMap<EventMessage>(cm =>
				{
					cm.MapProperty(p => p.Headers).SetElementName("h");
					cm.MapProperty(p => p.Body).SetElementName("b");
				});
			}
			var connectionString = this.TransformConnectionString(this.GetConnectionString());
			var database = MongoDatabase.Create(connectionString);
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