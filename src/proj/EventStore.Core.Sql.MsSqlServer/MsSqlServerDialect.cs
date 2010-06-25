namespace EventStore.Core.Sql.MsSqlServer
{
	using System.Data.Common;

	public sealed class MsSqlServerDialect : SqlDialect
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
			get { return MsSqlServerStatements.SelectEvents; }
		}
		public override string SelectEventsWhere
		{
			get { return MsSqlServerStatements.SelectEventsWhere; }
		}
		public override string InsertEvents
		{
			get { return MsSqlServerStatements.InsertEvents; }
		}
		public override string InsertEvent
		{
			get { return MsSqlServerStatements.InsertEvent; }
		}

		public override bool IsConcurrencyException(DbException exception)
		{
			return exception.ErrorCode == PrimaryKeyViolation || exception.ErrorCode == UniqueIndexViolation;
		}
	}
}