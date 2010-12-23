namespace EventStore.Persistence.AcceptanceTests
{
	using System;

	public interface IPersistenceFactory : IDisposable
	{
		IPersistStreams Build();
	}
}