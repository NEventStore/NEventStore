namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using System.Transactions;
	using RavenPersistence;
	using Serialization;

	public class AcceptanceTestRavenPersistenceFactory : RavenPersistenceFactory
	{
		private const bool FullyConsistentResults = true; // helps tests pass consistently
		private static readonly int PageSize = int.Parse("pageSize".GetSetting() ?? "10"); // smaller values help bring out bugs

		public AcceptanceTestRavenPersistenceFactory()
			: base("Raven", new DocumentObjectSerializer(), TransactionScopeOption.Suppress, FullyConsistentResults, PageSize)
		{
		}
	}
}