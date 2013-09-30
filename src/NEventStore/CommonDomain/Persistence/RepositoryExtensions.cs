namespace CommonDomain.Persistence
{
	using System;

	public static class RepositoryExtensions
	{
		public static void Save(this IRepository repository, IAggregate aggregate, Guid commitId)
		{
			repository.Save(aggregate, commitId, a => { });
		}

		public static void Save(this IRepository repository, string bucketId, IAggregate aggregate, Guid commitId)
		{
			repository.Save(bucketId, aggregate, commitId, a => { });
		}
	}
}