namespace EventStore.Persistence
{
	/// <summary>
	/// Whether the storage engine should track streams to snapshot
	/// (i.e. support the 'GetStreamsToSnapshot(int maxThreshold)' method)
	/// </summary>
	/// <remarks>
	/// For some storage engines this can add additional overhead which may be unnecessary
	/// if the application can decide what to snapshot in other ways so it's useful to be
	/// able to enable or disable this functionality as required.
	/// Although relying on the EventStore to return the list of streams to snapshot is
	/// convenient, it is based on simple count of events since the last snapshot which cannot 
	/// take into account specifics of the domain (aggregate types, size of events, size of 
	/// snapshot state, frequency of loading etc...).
	/// </remarks>
	public enum SnapshotTracking
	{
		/// <summary>
		/// Tracking is enabled and 'GetStreamsToSnapshot(int maxThreshold)' method supported
		/// </summary>
		Enabled,

		/// <summary>
		/// Tracking is disabled and 'GetStreamsToSnapshot(int maxThreshold)' method unsupported
		/// </summary>
		Disabled
	}
}
