namespace EventStore.Persistence.AcceptanceTests.MongoPersistence
{
	using System.Configuration;
	using Norm;
	using Persistence.MongoPersistence;
	using Serialization;

	public class MongoPersistenceFactory : IPersistenceFactory
	{
		public virtual IPersistStreams Build()
		{
			var connectionString = GetMongoConnectionString();
			var mongo = Mongo.Create(connectionString);
			return new MongoPersistenceEngine(mongo, new BinarySerializer());
		}
		private static string GetMongoConnectionString()
		{
			return ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString
				.Replace("[HOST]", "host".GetSetting() ?? "localhost")
				.Replace("[PORT]", "port".GetSetting() ?? string.Empty)
				.Replace("[DATABASE]", "database".GetSetting() ?? "EventStore2")
				.Replace("[USER]", "user".GetSetting() ?? string.Empty)
				.Replace("[PASSWORD]", "password".GetSetting() ?? string.Empty);
		}
	}
}