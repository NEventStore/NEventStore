namespace EventStore.SqlPersistence
{
	using System;
	using System.Data;

	public interface IConnectionFactory
	{
		IDbConnection Open(Guid streamId);
	}
}