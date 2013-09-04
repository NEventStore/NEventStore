namespace NEventStore.Persistence.RavenDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    // ReSharper disable InconsistentNaming
    public class when_querying_within_ambient_transaction : using_raven_persistence_with_ambient_transaction
    {
        Exception exception;

        protected override void Because()
        {
            exception = Catch.Exception(() => ravenPersistence.GetUndispatchedCommits().ToList());
        }

        [Fact]
        public void should_not_throw_exception_when_querying()
        {
            exception.ShouldBeNull();
        }
    }

    public class using_raven_persistence_with_ambient_transaction : SpecificationBase, IUseFixture<RavenAmbientTransactionFixture>
    {
        private TransactionScope ambientTransaction;
        protected IPersistStreams ravenPersistence;

        protected override void Context()
        {
            ravenPersistence = Data.EventStoreUsingAmbientTransaction(); 
           ambientTransaction = new TransactionScope(); 
            
        }

        protected override void Cleanup()
        {
            ambientTransaction.Complete();
            ambientTransaction.Dispose();
        }


        public void SetFixture(RavenAmbientTransactionFixture data)
        {
            Data = data;
        }

        protected RavenAmbientTransactionFixture Data { get; private set; }
    }

    public class RavenAmbientTransactionFixture : IDisposable
    {
        protected List<IPersistStreams> instantiatedPersistence = new List<IPersistStreams>();

        public IPersistStreams EventStoreUsingAmbientTransaction()
        {
            var config = TestRavenConfig.GetDefaultConfig();
            config.ScopeOption = TransactionScopeOption.Required; // use an existing transaction-scope, if available

            var persistence = new InMemoryRavenPersistenceFactory(config).Build();
            persistence.Initialize();

            return persistence;
        }

        public void Dispose()
        {
            foreach (var persistence in instantiatedPersistence)
            {
                persistence.Dispose();
            }
        }
    }
// ReSharper restore InconsistentNaming
}