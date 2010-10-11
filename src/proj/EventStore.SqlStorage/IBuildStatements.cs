namespace EventStore.SqlStorage
{
	using System;
	using System.Data;
	using System.Data.Common;

	public interface IBuildStatements
	{
		IDbCommand BuildLoadByIdQuery(Guid id, long maxStartingVersion);
		IDbCommand BuildLoadByCommandIdQuery(Guid commandId);
		IDbCommand BuildLoadStartingAfterQuery(Guid id, long version);
		IDbCommand BuildSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer);

		bool IsConstraintViolation(DbException exception);
		bool IsDuplicateKey(DbException exception);
	}
}