namespace EventStore.Dispatcher
{
	using System.Collections.Generic;

	/// <summary>
	/// Indicates the ability to keep track of which commits have or have not been dispatched.
	/// </summary>
	public interface ITrackDispatchedCommits
	{
		/// <summary>
		/// Gets a set of commits that has not yet been dispatched.
		/// </summary>
		/// <returns>The set of commits to be dispatched.</returns>
		IEnumerable<Commit> GetUndispatchedCommits();

		/// <summary>
		/// Marks the commit specified as dispatched.
		/// </summary>
		/// <param name="commit">The commit to be marked as dispatched.</param>
		void MarkCommitAsDispatched(Commit commit);
	}
}