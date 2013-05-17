using EventStore.Persistence.AcceptanceTests.RavenPersistence;

namespace EventStore.Persistence.AcceptanceTests.Engines
{
    using Persistence.RavenPersistence;

    public class AcceptanceTestRavenPersistenceFactory : RavenPersistenceFactory
	{
	    public AcceptanceTestRavenPersistenceFactory()
			: base(TestRavenConfig.GetDefaultConfig())
		{
		}

        public AcceptanceTestRavenPersistenceFactory(RavenConfiguration config)
            : base(config)
        {
        }

	}


}