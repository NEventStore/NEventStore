namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using MongoDBPersistence;
	using Serialization;

	public class AcceptanceTestMongoPersistenceFactory : MongoDBPersistenceFactory
	{
		public AcceptanceTestMongoPersistenceFactory()
			: base("Mongo", new BinarySerializer())
		{
		}
		protected override string TransformConnectionString(string connectionString)
		{
			return connectionString
				.Replace("[HOST]", "host".GetSetting() ?? "localhost")
				.Replace("[PORT]", "port".GetSetting() ?? string.Empty)
				.Replace("[DATABASE]", "database".GetSetting() ?? "EventStore2b")
				.Replace("[USER]", "user".GetSetting() ?? string.Empty)
				.Replace("[PASSWORD]", "password".GetSetting() ?? string.Empty);
		}
	}
}