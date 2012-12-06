using System.Transactions;
using EventStore.Logging;
using EventStore.Serialization;

namespace EventStore.Persistence.AzureTablesPersistence.Wireup
{
    public class AzureTablesPersistenceWireup : PersistenceWireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof(AzureTablesPersistenceWireup));

        public AzureTablesPersistenceWireup(EventStore.Wireup inner, string connectionStringName)
            : base(inner)
        {
            Logger.Debug("Configuring Windows Azure Tables persistence engine");

            var options = this.Container.Resolve<TransactionScopeOption>();
            if (options != TransactionScopeOption.Suppress)
                Logger.Warn("Windows Azure Tables does not participate in transactions using TransactionScope.");

            this.Container.Register(c => new AzureTablesPersistenceFactory(connectionStringName, c.Resolve<ISerialize>()).Build());
        }
    }
}
