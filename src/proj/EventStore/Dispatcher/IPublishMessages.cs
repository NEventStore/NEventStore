namespace EventStore.Dispatcher
{
	using Persistence;

	/// <summary>
	/// Indicates the ability to publish the commit to the underlying messaging infrastructure.
	/// </summary>
	public interface IPublishMessages
	{
		/// <summary>
		/// Breaks apart the commit into a series of messages and provides them to the messaging system.
		/// </summary>
		/// <param name="commit">The commit whose messages are to be published.</param>
		void Publish(Commit commit);
	}
}