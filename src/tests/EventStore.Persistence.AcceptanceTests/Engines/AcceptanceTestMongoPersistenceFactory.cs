namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using MongoPersistence;
	using Serialization;

	public class AcceptanceTestMongoPersistenceFactory : MongoPersistenceFactory
	{
		public AcceptanceTestMongoPersistenceFactory()
			: base("Mongo", new DocumentObjectSerializer())
		{
		}
		protected override string TransformConnectionString(string connectionString)
		{
			return connectionString
				.Replace("[HOST]", "host".GetSetting() ?? "localhost")
				.Replace("[PORT]", "port".GetSetting() ?? string.Empty)
				.Replace("[DATABASE]", "database".GetSetting() ?? "EventStore")
				.Replace("[USER]", "user".GetSetting() ?? string.Empty)
				.Replace("[PASSWORD]", "password".GetSetting() ?? string.Empty);
		}
	}
}