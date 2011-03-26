namespace EventStore
{
	/// <summary>
	/// Provides the ability to hook into the pipeline for selecting a commit from persistence.
	/// </summary>
	public interface IReadHook
	{
		/// <summary>
		/// Hooks into the selection pipeline just prior to the commit being returned to the caller.
		/// </summary>
		/// <param name="committed">The commit to be filtered.</param>
		/// <returns>If successful, returns a populated commit; otherwise returns null.</returns>
		Commit Select(Commit committed);
	}
}