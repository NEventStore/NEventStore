namespace EventStore
{
	/// <summary>
	/// Provides the ability to hook into the pipeline of persisting a commit.
	/// </summary>
	public interface IHookCommitAttempts
	{
		/// <summary>
		/// Hooks into the commit pipeline prior to persisting the commit to durable storage.
		/// </summary>
		/// <param name="attempt">The attempt to be committed.</param>
		/// <returns>If processing should continue, returns true; otherwise returns false.</returns>
		bool PreCommit(Commit attempt);

		/// <summary>
		/// Hooks into the commit pipeline just after the commit has been *successfully* committed to durable storage.
		/// </summary>
		/// <param name="persisted">The commit which has been persisted.</param>
		void PostCommit(Commit persisted);
	}
}