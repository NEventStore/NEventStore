namespace EventStore.Dispatcher
{
	using System;
	using Persistence;

	/// <summary>
	/// Indicates the ability to dispatch or publish all messages associated with a particular commit.
	/// </summary>
	public interface IDispatchCommits : IDisposable
	{
		/// <summary>
		/// Dispatches the series of messages contained within the commit provided to all interested parties.
		/// </summary>
		/// <param name="commit">The commit representing the series of messages to dispatch.</param>
		void Dispatch(Commit commit);
	}
}