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
		public virtual string CorrelationId
		{
			get { return "@correlation_id"; }
		}
		public virtual string CorrelationPayload
		{
			get { return "@correlation_payload"; }
		}

		public abstract string SelectEvents { get; }
		public virtual string SelectEventsForCorrelation
		{
			get { return SqlStatements.SelectEventsForCorrelation; }
		}
		public virtual string SelectEventsForVersion
		{
			get { return SqlStatements.SelectEventsForVersion; }
		}
		public virtual string InsertEvents
		{
			get { return SqlStatements.InsertEvents; }
		}
		public virtual string InsertEvent
		{
			get { return SqlStatements.InsertEvent; }
		}

		public abstract bool IsConcurrencyException(DbException exception);
	}
}