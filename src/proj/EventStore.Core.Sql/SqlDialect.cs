namespace EventStore.Core.Sql
{
	using System.Data.Common;

	public abstract class SqlDialect
	{
		public virtual string Id
		{
			get { return "@id"; }
		}
		public virtual string InitialVersion
		{
			get { return "@initial_version"; }
		}
		public virtual string CurrentVersion
		{
			get { return "@current_version"; }
		}
		public virtual string Type
		{
			get { return "@type"; }
		}
		public virtual string Created
		{
			get { return "@created"; }
		}
		public virtual string Payload
		{
			get { return "@payload"; }
		}
		public virtual string SnapshotType
		{
			get { return "@snapshot_type"; }
		}

		public abstract string SelectEvents { get; }
		public abstract string SelectEventsWhere { get; }
		public abstract string InsertEvents { get; }
		public abstract string InsertEvent { get; }

		public abstract bool IsConcurrencyException(DbException exception);
	}
}