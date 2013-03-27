namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public interface IConnectionFactory
	{
		IDbConnection OpenMaster(Guid streamId);
		IDbConnection OpenReplica(Guid streamId);
	}
}