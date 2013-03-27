

namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using System.Transactions;
	using Serialization;
    using Persistence.RavenPersistence;

	public class AcceptanceTestRavenPersistenceFactory : RavenPersistenceFactory
	{
        public static RavenConfiguration GetDefaultConfig()
        {
            return new RavenConfiguration
            {
                Serializer = new DocumentObjectSerializer(),
                ScopeOption = TransactionScopeOption.Suppress,
                ConsistentQueries = true, // helps tests pass consistently
                RequestedPageSize = int.Parse("pageSize".GetSetting() ?? "10"), // smaller values help bring out bugs
                MaxServerPageSize = int.Parse("serverPageSize".GetSetting() ?? "1024"), // raven default
                ConnectionName = "Raven"
            };
        }

		public AcceptanceTestRavenPersistenceFactory()
			: base(GetDefaultConfig())
		{
		}

        public AcceptanceTestRavenPersistenceFactory(RavenConfiguration config)
            : base(config)
        {
        }
	}
}