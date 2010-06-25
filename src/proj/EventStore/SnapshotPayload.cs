namespace EventStore
{
	using System;

	/// <summary>
	/// The payload representing the snapshot.
	/// </summary>
	public class SnapshotPayload
	{
		/// <summary>
		/// Gets or sets the value which uniquely identifies the aggregate to which the snapshot belongs.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the version of the snapshot.
		/// </summary>
		public int Version { get; set; }

		/// <summary>
		/// Gets or sets the memento which represents the snapshot.
		/// </summary>
		public object Memento { get; set; }
	}
}