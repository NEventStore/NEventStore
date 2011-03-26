namespace EventStore
{
	/// <summary>
	/// Provides the ability to hook into the pipeline of persisting a commit.
	/// </summary>
	public interface IPipelineHook
	{
		/// <summary>
		/// Hooks into the selection pipeline just prior to the commit being returned to the caller.
		/// </summary>
		/// <param name="committed">The commit to be filtered.</param>
		/// <returns>If successful, returns a populated commit; otherwise returns null.</returns>
		Commit Select(Commit committed);

		/// <summary>
		/// Hooks into the commit pipeline prior to persisting the commit to durable storage.
		/// </summary>
		/// <param name="attempt">The attempt to be committed.</param>
		/// <returns>If processing should continue, returns true; otherwise returns false.</returns>
		bool PreCommit(Commit attempt);

		/// <summary>
		/// Hooks into the commit pipeline just after the commit has been *successfully* committed to durable storage.
		/// </summary>
		/// <param name="committed">The commit which has been persisted.</param>
		void PostCommit(Commit committed);
	}
}