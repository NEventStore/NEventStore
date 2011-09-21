namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using System.Transactions;
	using RavenPersistence;
	using Serialization;

	public class AcceptanceTestRavenPersistenceFactory : RavenPersistenceFactory
	{
		private static readonly RavenConfiguration Config = new RavenConfiguration
		{
			Serializer = new DocumentObjectSerializer(),
			ScopeOption = TransactionScopeOption.Suppress,
			ConsistentQueries = true, // helps tests pass consistently
			RequestedPageSize = int.Parse("pageSize".GetSetting() ?? "10"), // smaller values help bring out bugs
			MaxServerPageSize = int.Parse("serverPageSize".GetSetting() ?? "1024"), // raven default
			ConnectionName = "Raven"
		};

		public AcceptanceTestRavenPersistenceFactory()
			: base(Config)
		{
		}
	}
}