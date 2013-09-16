// ReSharper disable CheckNamespace

namespace NEventStore // ReSharper restore CheckNamespace
{
    using System;
    using System.Configuration;
    using NEventStore.Persistence.MongoPersistence;
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