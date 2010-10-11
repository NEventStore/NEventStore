namespace EventStore.Core.SqlStorage
{
	using System;
	using System.Data;
	using System.Data.Common;

	public interface IPrepareStatements
	{
		IDbCommand PrepareLoadByIdQuery(Guid id, long maxStartingVersion);
		IDbCommand PrepareLoadByCommandIdQuery(Guid commandId);
		IDbCommand PrepareLoadStartingAfterQuery(Guid id, long version);
		IDbCommand PrepareSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer);

		bool IsConstraintViolation(DbException exception);
		bool IsDuplicateKey(DbException exception);
	}
}