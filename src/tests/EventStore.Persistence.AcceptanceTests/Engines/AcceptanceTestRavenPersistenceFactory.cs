namespace EventStore.Persistence.AcceptanceTests.Engines
{
	using RavenPersistence;
	using Serialization;

	public class AcceptanceTestRavenPersistenceFactory : RavenPersistenceFactory
	{
		public AcceptanceTestRavenPersistenceFactory()
			: base("Raven", new BinarySerializer())
		{
		}
	}
}