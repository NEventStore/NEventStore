namespace EventStore.Persistence
{
	/// <summary>
	/// Indicates the ability to build a ready-to-use persistence engine.
	/// </summary>
	public interface IPersistenceFactory
	{
		/// <summary>
		/// Builds a persistence engine.
		/// </summary>
		/// <returns>A ready-to-use persistence engine.</returns>
		IPersistStreams Build();
	}
}