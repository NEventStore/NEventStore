namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using System.Configuration;
	using RavenPersistence;

	public class AcceptanceTestRavenPersistenceFactory : RavenPersistenceFactory
	{
		public AcceptanceTestRavenPersistenceFactory()
			: base("RavenDB")
		{
		}
		protected override string GetRavenUrl()
		{
			return ConfigurationManager.ConnectionStrings["RavenDB"].ConnectionString
				.Replace("[HOST]", "host".GetSetting() ?? "localhost")
				.Replace("[PORT]", "port".GetSetting() ?? string.Empty)
				.Replace("[USER]", "user".GetSetting() ?? string.Empty)
				.Replace("[PASSWORD]", "password".GetSetting() ?? string.Empty);
		}
		protected override string GetRavenDatabase()
		{
			return "database".GetSetting() ?? "EventStore2";
		}
	}
}