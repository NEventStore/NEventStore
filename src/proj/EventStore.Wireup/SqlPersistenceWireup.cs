namespace EventStore
{
	using Persistence.SqlPersistence;
	using Serialization;

	public class SqlPersistenceWireup : PersistenceWireup
	{
		private readonly IConnectionFactory connectionFactory;
		private ISerialize serializer = new BinarySerializer();
		private ISqlDialect dialect; // auto-detect by default

		public SqlPersistenceWireup(Wireup wireup, IConnectionFactory connectionFactory)
			: base(wireup)
		{
			this.connectionFactory = connectionFactory;
		}

		public virtual SqlPersistenceWireup WithSerializer(ISerialize instance)
		{
			this.serializer = instance; // TODO: null check
			return this.Prebuild();
		}

		public virtual SqlPersistenceWireup WithDialect(ISqlDialect instance)
		{
			this.dialect = instance; // TODO: null check
			return this.Prebuild();
		}

		protected virtual SqlPersistenceWireup Prebuild()
		{
			var factory = new SqlPersistenceFactory(this.connectionFactory, this.serializer, this.dialect);
			this.WithPersistence(factory.Build());
			return this;
		}
	}
}