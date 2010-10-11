namespace EventStore.SqlStorage
{
	using System;
	using System.Data;

	public interface IBuildStatements
	{
		IAdaptSqlDialect Dialect { get; }

		IDbCommand BuildLoadByIdQuery(Guid id, long maxStartingVersion);
		IDbCommand BuildLoadByCommandIdQuery(Guid commandId);
		IDbCommand BuildLoadStartingAfterQuery(Guid id, long version);
		IDbCommand BuildSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer);
	}
}