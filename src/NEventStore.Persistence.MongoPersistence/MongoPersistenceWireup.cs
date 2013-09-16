namespace NEventStore.Persistence.MongoPersistence
{
    using System;
    using System.Transactions;
    using NEventStore.Logging;
    using NEventStore.Serialization;

    public class MongoPersistenceWireup : PersistenceWireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (MongoPersistenceWireup));

        public MongoPersistenceWireup(Wireup inner, Func<string> connectionStringProvider, IDocumentSerializer serializer)
            : base(inner)
        {
            Logger.Debug("Configuring Mongo persistence engine.");

            var options = Container.Resolve<TransactionScopeOption>();
            if (options != TransactionScopeOption.Suppress)
            {
                Logger.Warn("MongoDB does not participate in transactions using TransactionScope.");
            }

            Container.Register(c => new MongoPersistenceFactory(connectionStringProvider, serializer).Build());
        }
    }
}