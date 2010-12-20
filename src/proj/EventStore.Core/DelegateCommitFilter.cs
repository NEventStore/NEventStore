namespace EventStore.Core
{
	using System;

	public class DelegateCommitFilter<T> : IFilterCommits<T>
	{
		private readonly Func<T, T> filter;

		public DelegateCommitFilter(Func<T, T> filter)
		{
			this.filter = filter;
		}

		public T Filter(T commit)
		{
			return this.filter(commit);
		}
	}
}