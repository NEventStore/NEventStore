namespace EventStore.Core
{
	using System;
	using Persistence;

	public class DelegateCommitFilter : IFilterCommits
	{
		private readonly Func<Commit, Commit> filter;

		public DelegateCommitFilter(Func<Commit, Commit> filter)
		{
			this.filter = filter;
		}

		public Commit Filter(Commit commit)
		{
			return this.filter(commit);
		}
	}
}