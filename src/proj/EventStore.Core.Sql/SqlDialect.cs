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
		public virtual string Payload
		{
			get { return "@payload"; }
		}
		public virtual string SnapshotType
		{
			get { return "@snapshot_type"; }
		}

		public virtual string SelectEventsWhere
		{
			get { return SqlStatements.SelectEventsWhere; }
		}
		public virtual string InsertEvents
		{
			get { return SqlStatements.InsertEvents; }
		}
		public virtual string InsertEvent
		{
			get { return SqlStatements.InsertEvent; }
		}

		public abstract string SelectEvents { get; }
		public abstract bool IsConcurrencyException(DbException exception);
	}
}