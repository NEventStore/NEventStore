namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public interface IConnectionFactory
	{
		IDbConnection Open(Guid streamId);
	}
}