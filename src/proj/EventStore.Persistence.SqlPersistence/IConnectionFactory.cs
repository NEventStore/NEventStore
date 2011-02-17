namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Configuration;
	using System.Data;

	public interface IConnectionFactory
	{
		ConnectionStringSettings Settings { get; }

		IDbConnection OpenMaster(Guid streamId);
		IDbConnection OpenSlave(Guid streamId);
	}
}