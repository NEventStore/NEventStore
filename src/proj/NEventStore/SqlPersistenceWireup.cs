namespace NEventStore
{
    using System.Transactions;
    using Logging;
    using Persistence.SqlPersistence;
    using Serialization;

    public class SqlPersistenceWireup : PersistenceWireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(SqlPersistenceWireup));
		private const int DefaultPageSize = 512;
		private int pageSize = DefaultPageSize;

		public SqlPersistenceWireup(Wireup wireup, IConnectionFactory connectionFactory)
			: base(wireup)
		{
			Logger.Debug(Messages.ConnectionFactorySpecified, connectionFactory);

			Logger.Verbose(Messages.AutoDetectDialect);
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
			Logger.Debug(Messages.DialectSpecified, instance.GetType());
			this.Container.Register(instance);
			return this;
		}

		public virtual SqlPersistenceWireup PageEvery(int records)
		{
			Logger.Debug(Messages.PagingSpecified, records);
			this.pageSize = records;
			return this;
		}
	}
}