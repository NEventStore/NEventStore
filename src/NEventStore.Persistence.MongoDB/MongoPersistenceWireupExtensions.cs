// ReSharper disable once CheckNamespace
namespace NEventStore
{
	using System;
	using System.Configuration;
	using NEventStore.Persistence.MongoDB;
	using NEventStore.Serialization;

	public static class MongoPersistenceWireupExtensions
	{
		public static PersistenceWireup UsingMongoPersistence(this Wireup wireup, string connectionName, IDocumentSerializer serializer, MongoPersistenceOptions options = null)
		{
			return new MongoPersistenceWireup(wireup, () => ConfigurationManager.ConnectionStrings[connectionName].ConnectionString, serializer, options);
		}

		public static PersistenceWireup UsingMongoPersistence(this Wireup wireup, Func<string> connectionStringProvider, IDocumentSerializer serializer, MongoPersistenceOptions options = null)
		{
			return new MongoPersistenceWireup(wireup, connectionStringProvider, serializer, options);
		}
	}
}