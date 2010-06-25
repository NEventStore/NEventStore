namespace EventStore.Core.Sql
{
	using System.Data.Common;

	public abstract class SqlDialect
	{
		public abstract string IdParameter { get; }
		public abstract string VersionParameter { get; }
		public abstract string CreatedParameter { get; }
		public abstract string PayloadParameter { get; }
		public abstract string RuntimeTypeParameter { get; }

		public abstract string LoadEvents { get; }
		public abstract string StoreEvents { get; }
		public abstract string StoreEvent { get; }
		public abstract string LoadSnapshot { get; }
		public abstract string StoreSnapshot { get; }

		public abstract bool IsConcurrencyException(DbException exception);
	}
}