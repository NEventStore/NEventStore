namespace EventStore.Persistence.AcceptanceTests.RavenPersistence
{
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
			var host = "host".GetSetting() ?? "localhost";
			var port = "port".GetSetting() ?? "8080";
			return "http://" + host + ":" + port;
		}
		private static string GetRavenDatabase()
		{
			return "database".GetSetting() ?? string.Empty;
		}
	}
}