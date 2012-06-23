namespace EventStore.Persistence
{
	/// <summary>
	/// Whether the storage engine should track commits to be dispatched
	/// (i.e. support the 'GetUndispatchedCommits()' and 'MarkCommitAsDispatched(Commit commit)' methods)
	/// </summary>
	/// <remarks>
	/// For some storage engines this can add additional overhead which may be unnecessary
	/// if the application is ensuring commits are dispatched another way (e.g. by keeping track
	/// of date/time the last dispatched message and re-dispatching events from then).
	/// The overhead of tracking and updating the dispatched flag can be significant for storage
	/// engines such as MongoDB and esp Azure in high-transaction scenarios. Also, when flags can
	/// cause issues if starting multiple nodes (where each node sees and submits the undispatched
	/// commits) 
	/// </remarks>
	public enum DispatchedTracking
	{
		/// <summary>
		/// Tracking is enabled and 'GetUndispatchedCommits()' and 'MarkCommitAsDispatched(Commit commit)' methods supported
		/// </summary>
		Enabled,

		/// <summary>
		/// Tracking is disabled and 'GetUndispatchedCommits()' and 'MarkCommitAsDispatched(Commit commit)' methods unsupported
		/// </summary>
		Disabled
	}
}