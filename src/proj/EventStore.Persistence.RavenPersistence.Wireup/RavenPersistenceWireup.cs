namespace EventStore
{
	using System.Transactions;
	using Persistence.RavenPersistence;
	using Serialization;

	public class RavenPersistenceWireup : PersistenceWireup
	{
		private int pageSize = 128;

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
				consistentQueries,
				this.pageSize).Build());
		}

		public virtual RavenPersistenceWireup PageEvery(int records)
		{
			this.pageSize = records;
			return this;
		}
	}
}