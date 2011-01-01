namespace EventStore.Persistence.AcceptanceTests.RavenPersistence
{
	using System.Configuration;
	using Persistence.RavenPersistence;
	using Raven.Client.Document;

	public class RavenPersistenceFactory : IPersistenceFactory
	{
		public IPersistStreams Build()
		{
			var store = new DocumentStore
			{
				Url = GetRavenUrl(),
				DefaultDatabase = GetRavenDatabase()
			};

			return new RavenPersistenceEngine(
				store,
				new RavenInitializer());
		}
		private static string GetRavenUrl()
		{
			return ConfigurationManager.ConnectionStrings["RavenDB"].ConnectionString
				.Replace("[HOST]", "host".GetSetting() ?? "localhost")
				.Replace("[PORT]", "port".GetSetting() ?? string.Empty)
				.Replace("[USER]", "user".GetSetting() ?? string.Empty)
				.Replace("[PASSWORD]", "password".GetSetting() ?? string.Empty);
		}
		private static string GetRavenDatabase()
		{
			return "database".GetSetting() ?? "EventStore2";
		}
	}
}