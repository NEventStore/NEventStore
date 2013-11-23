// ReSharper disable CheckNamespace

namespace NEventStore // ReSharper restore CheckNamespace
{
    using System;
    using System.Transactions;
    using NEventStore.Logging;
    using NEventStore.Persistence.MongoDB;
    using NEventStore.Serialization;

    public class MongoPersistenceWireup : PersistenceWireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (MongoPersistenceWireup));

        public MongoPersistenceWireup(Wireup inner, Func<string> connectionStringProvider, IDocumentSerializer serializer, MongoPersistenceOptions persistenceOptions)
            : base(inner)
        {
            Logger.Debug("Configuring Mongo persistence engine.");

            var options = Container.Resolve<TransactionScopeOption>();
            if (options != TransactionScopeOption.Suppress)
            {
                Logger.Warn("MongoDB does not participate in transactions using TransactionScope.");
            }

			Container.Register(c => new MongoPersistenceFactory(connectionStringProvider, serializer, persistenceOptions).Build());
        }
    }
}
