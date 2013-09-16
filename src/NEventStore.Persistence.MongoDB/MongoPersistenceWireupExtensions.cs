// ReSharper disable once CheckNamespace
namespace NEventStore
{
    using System;
    using System.Configuration;
    using NEventStore.Serialization;

    public static class MongoPersistenceWireupExtensions
    {
        public static PersistenceWireup UsingMongoPersistence(this Wireup wireup, string connectionName, IDocumentSerializer serializer)
        {
            return new MongoPersistenceWireup(wireup, () => ConfigurationManager.ConnectionStrings[connectionName].ConnectionString, serializer);
        }

        public static PersistenceWireup UsingMongoPersistence(this Wireup wireup, Func<string> connectionStringProvider, IDocumentSerializer serializer)
        {
            return new MongoPersistenceWireup(wireup, connectionStringProvider, serializer);
        }
    }
}