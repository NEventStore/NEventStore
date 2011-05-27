namespace EventStore
{
	using System.Transactions;
	using Persistence.SqlPersistence;
	using Serialization;

	public class SqlPersistenceWireup : PersistenceWireup
	{
		public SqlPersistenceWireup(Wireup wireup, IConnectionFactory connectionFactory)
			: base(wireup)
		{
			this.Container.Register<ISqlDialect>(c => null); // auto-detect

			this.Container.Register(c => new SqlPersistenceFactory(
				connectionFactory,
				c.Resolve<ISerialize>(),
				c.Resolve<ISqlDialect>(),
				c.Resolve<TransactionScopeOption>()).Build());
		}

		public virtual SqlPersistenceWireup WithDialect(ISqlDialect instance)
		{
			this.Container.Register(instance);
			return this;
		}
	}
}