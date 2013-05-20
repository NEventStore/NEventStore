using System;
using EventStore.Persistence.RavenPersistence.Tests;

namespace EventStore.Persistence.AcceptanceTests
{
    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.CreatePersistence = () => 
                new InMemoryRavenPersistenceFactory(TestRavenConfig.GetDefaultConfig()).Build();
        }
    }
}