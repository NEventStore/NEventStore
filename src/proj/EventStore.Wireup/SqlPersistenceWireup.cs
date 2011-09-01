namespace EventStore
{
	using System.Transactions;
	using Persistence.SqlPersistence;
	using Serialization;

	public class SqlPersistenceWireup : PersistenceWireup
	{
		private const int DefaultPageSize = 128;
		private int pageSize = DefaultPageSize;

		public SqlPersistenceWireup(Wireup wireup, IConnectionFactory connectionFactory)
			: base(wireup)
		{
			this.Container.Register<ISqlDialect>(c => null); // auto-detect

			this.Container.Register(c => new SqlPersistenceFactory(
				connectionFactory,
				c.Resolve<ISerialize>(),
				c.Resolve<ISqlDialect>(),
				c.Resolve<TransactionScopeOption>(),
				this.pageSize).Build());
		}

		public virtual SqlPersistenceWireup WithDialect(ISqlDialect instance)
		{
			this.Container.Register(instance);
			return this;
		}

		public virtual SqlPersistenceWireup PageEvery(int records)
		{
			this.pageSize = records;
			return this;
		}
	}
}