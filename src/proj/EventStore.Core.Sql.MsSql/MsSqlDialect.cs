namespace EventStore.Core.Sql.MsSql
{
	using System.Data.Common;

	public sealed class MsSqlDialect : SqlDialect
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;

		public override string Id
		{
			get { return "@id"; }
		}
		public override string Version
		{
			get { return "@version"; }
		}
		public override string Type
		{
			get { return "@type"; }
		}
		public override string Created
		{
			get { return "@created"; }
		}
		public override string Payload
		{
			get { return "@payload"; }
		}
		public override string MomentoType
		{
			get { return "@momento_type"; }
		}

		public override string SelectEvents
		{
			get { return MsSqlStatements.SelectEvents; }
		}
		public override string SelectEventsWhere
		{
			get { return MsSqlStatements.SelectEventsWhere; }
		}
		public override string InsertEvents
		{
			get { return MsSqlStatements.InsertEvents; }
		}
		public override string InsertEvent
		{
			get { return MsSqlStatements.InsertEvent; }
		}

		public override bool IsConcurrencyException(DbException exception)
		{
			return exception.ErrorCode == PrimaryKeyViolation || exception.ErrorCode == UniqueIndexViolation;
		}
	}
}