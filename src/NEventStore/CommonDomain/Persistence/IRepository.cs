namespace CommonDomain.Persistence
{
	using System;
	using System.Collections.Generic;

	public interface IRepository : IDisposable
	{
		TAggregate GetById<TAggregate>(Guid id) where TAggregate : class, IAggregate;

		TAggregate GetById<TAggregate>(Guid id, int version) where TAggregate : class, IAggregate;

		TAggregate GetById<TAggregate>(string bucketId, Guid id) where TAggregate : class, IAggregate;

		TAggregate GetById<TAggregate>(string bucketId, Guid id, int version) where TAggregate : class, IAggregate;

		void Save(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders);

		void Save(string bucketId, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders);
	}
}