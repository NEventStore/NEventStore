namespace EventStore.Persistence.RavenPersistence
{
	using Raven.Client;

	public interface IInitializeRaven
	{
		void Initialize(IDocumentStore store);
	}
}