namespace NEventStore.Persistence.RavenPersistence.Tests
{
    using System;
    using System.Transactions;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Serialization;

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