namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public interface IConnectionFactory
	{
		IDbConnection OpenForReading(Guid streamId);
		IDbConnection OpenForWriting(Guid streamId);
	}
}