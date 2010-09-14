namespace EventStore.Core.SqlStorage
{
	using System.Data;
	using System.Data.Common;

	public interface ISqlDialect
	{
		string Id { get; }
		string TenantId { get; }
		string InitialVersion { get; }
		string CurrentVersion { get; }
		string Type { get; }
		string Payload { get; }
		string SnapshotType { get; }
		string CommandId { get; }
		string CommandPayload { get; }
		string SelectEvents { get; }
		string SelectEventsForCommand { get; }
		string SelectEventsForVersion { get; }
		string InsertEvents { get; }
		string InsertEvent { get; }

		IDbCommand CreateCommand(string commandText);
		bool IsConstraintViolation(DbException exception);
		bool IsDuplicateKey(DbException exception);
	}
}