namespace NEventStore
{
    using System.Transactions;
    using NEventStore.Logging;
    using NEventStore.Persistence.SqlPersistence;
    using NEventStore.Serialization;

    public class SqlPersistenceWireup : PersistenceWireup
    {
        private const int DefaultPageSize = 512;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (SqlPersistenceWireup));
        private int _pageSize = DefaultPageSize;

        public SqlPersistenceWireup(Wireup wireup, IConnectionFactory connectionFactory)
            : base(wireup)
        {
            Logger.Debug(Messages.ConnectionFactorySpecified, connectionFactory);

            Logger.Verbose(Messages.AutoDetectDialect);
            Container.Register<ISqlDialect>(c => null); // auto-detect

            Container.Register(c => new SqlPersistenceFactory(
                connectionFactory,
                c.Resolve<ISerialize>(),
                c.Resolve<ISqlDialect>(),
                c.Resolve<TransactionScopeOption>(),
                _pageSize).Build());
        }

        public virtual SqlPersistenceWireup WithDialect(ISqlDialect instance)
        {
            Logger.Debug(Messages.DialectSpecified, instance.GetType());
            Container.Register(instance);
            return this;
        }

        public virtual SqlPersistenceWireup PageEvery(int records)
        {
            Logger.Debug(Messages.PagingSpecified, records);
            _pageSize = records;
            return this;
        }
    }
}