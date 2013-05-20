using System;
using System.Transactions;
using EventStore.Persistence.AcceptanceTests;
using EventStore.Serialization;

namespace EventStore.Persistence.RavenPersistence.Tests
{
    public static class TestRavenConfig
    {
        public static RavenConfiguration GetDefaultConfig()
        {
            return new RavenConfiguration
                   {
                       Serializer = new DocumentObjectSerializer(),
                       ScopeOption = TransactionScopeOption.Suppress,
                       ConsistentQueries = true, // helps tests pass consistently
                       RequestedPageSize = Int32.Parse("pageSize".GetSetting() ?? "10"), // smaller values help bring out bugs
                       MaxServerPageSize = Int32.Parse("serverPageSize".GetSetting() ?? "1024"), // raven default
                       ConnectionName = "Raven"
                   };
        }
    }
}