namespace EventStore.Core.Sql
{
	using System.Data.Common;

	public abstract class SqlDialect
	{
		public abstract string Id { get; }
		public abstract string Version { get; }
		public abstract string Type { get; }
		public abstract string Created { get; }
		public abstract string Payload { get; }
		public abstract string MomentoType { get; }

		public abstract string SelectEvents { get; }
		public abstract string SelectEventsWhere { get; }
		public abstract string InsertEvents { get; }
		public abstract string InsertEvent { get; }

		public abstract bool IsConcurrencyException(DbException exception);
	}
}