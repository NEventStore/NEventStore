namespace EventStore.Persistence.AcceptanceTests
{
	public interface IPersistenceFactory
	{
		IPersistStreams Build();
	}
}