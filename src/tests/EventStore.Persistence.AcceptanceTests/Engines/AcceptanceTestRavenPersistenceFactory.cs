namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using System.Transactions;
	using RavenPersistence;
	using Serialization;

	public class AcceptanceTestRavenPersistenceFactory : RavenPersistenceFactory
	{
		private const bool FullyConsistentResults = true;
		public AcceptanceTestRavenPersistenceFactory()
			: base("Raven", new DocumentObjectSerializer(), TransactionScopeOption.Suppress, FullyConsistentResults)
		{
		}
	}
}