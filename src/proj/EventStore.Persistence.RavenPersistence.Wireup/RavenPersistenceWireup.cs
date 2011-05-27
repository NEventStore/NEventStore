namespace EventStore
{
	using System.Transactions;
	using Persistence.RavenPersistence;
	using Serialization;

	public class RavenPersistenceWireup : PersistenceWireup
	{
		public RavenPersistenceWireup(
			Wireup inner,
			string connectionName,
			IDocumentSerializer serializer,
			bool consistentQueries)
			: base(inner)
		{
			this.Container.Register(c => new RavenPersistenceFactory(
				connectionName,
				serializer,
				this.Container.Resolve<TransactionScopeOption>(),
				consistentQueries).Build());
		}
	}
}