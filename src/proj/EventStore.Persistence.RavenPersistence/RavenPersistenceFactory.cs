namespace EventStore.Persistence.RavenPersistence
{
	using System.Configuration;
	using Raven.Client.Document;

	public class RavenPersistenceFactory : IPersistenceFactory
	{
		private readonly string connectionName;

		public RavenPersistenceFactory(string connectionName)
		{
			this.connectionName = connectionName;
		}

		public virtual IPersistStreams Build()
		{
			return new RavenPersistenceEngine(this.BuildStore(), new RavenInitializer());
		}
		protected virtual DocumentStore BuildStore()
		{
			return new DocumentStore
			{
				Url = this.GetRavenUrl(),
				DefaultDatabase = this.GetRavenDatabase()
			};
		}
		protected virtual string GetRavenUrl()
		{
			return ConfigurationManager.ConnectionStrings[this.connectionName].ConnectionString;
		}
		protected virtual string GetRavenDatabase()
		{
			return string.Empty;
		}
	}
}