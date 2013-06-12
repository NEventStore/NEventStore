using System;
using EventStore.Persistence.MongoPersistence.Tests;

namespace EventStore.Persistence.AcceptanceTests
{
    public partial class PersistenceEngineFixture
    {
        public PersistenceEngineFixture()
        {
            this.CreatePersistence = () => 
                new AcceptanceTestMongoPersistenceFactory().Build();

            PurgeOnDispose = true;
        }
    }
}