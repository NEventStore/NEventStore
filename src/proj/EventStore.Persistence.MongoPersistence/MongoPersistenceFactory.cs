namespace EventStore.Persistence.MongoPersistence
{
	using System.Configuration;
	using Norm;
	using Serialization;

	public class MongoPersistenceFactory : IPersistenceFactory
	{
		private readonly string connectionName;
		private readonly ISerialize serializer;

		public MongoPersistenceFactory(string connectionName, ISerialize serializer)
		{
			this.connectionName = connectionName;
			this.serializer = serializer;
		}

		public virtual IPersistStreams Build()
		{
			var connectionString = this.TransformConnectionString(this.GetConnectionString());
			var engine = Mongo.Create(connectionString);
			return new MongoPersistenceEngine(engine, this.serializer);
		}
		protected virtual string GetConnectionString()
		{
			return ConfigurationManager.ConnectionStrings[this.connectionName].ConnectionString;
		}
		protected virtual string TransformConnectionString(string connectionString)
		{
			return connectionString;
		}
	}
}