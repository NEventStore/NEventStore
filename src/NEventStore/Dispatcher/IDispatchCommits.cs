namespace NEventStore.Dispatcher
{
    using System;

    /// <summary>
	/// Indicates the ability to dispatch the specified commit to some kind of communications infrastructure.
	/// </summary>
	/// <remarks>
	/// Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
	/// </remarks>
	public interface IDispatchCommits : IDisposable
	{
		/// <summary>
		/// Dispatches the commit specified to the messaging infrastructure.
		/// </summary>
		/// <param name="commit">The commmit to be dispatched.</param>
		void Dispatch(Commit commit);
	}
}