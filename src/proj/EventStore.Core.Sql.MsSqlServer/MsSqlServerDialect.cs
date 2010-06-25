namespace EventStore.Core.Sql.MsSqlServer
{
	using System.Data.Common;

	public sealed class MsSqlServerDialect : SqlDialect
	{
		private const int PrimaryKeyViolation = 2627;
		private const int UniqueIndexViolation = 2601;

		public override string IdParameter
		{
			get { return "@id"; }
		}
		public override string VersionParameter
		{
			get { return "@version"; }
		}
		public override string RuntimeTypeParameter
		{
			get { return "@type"; }
		}
		public override string CreatedParameter
		{
			get { return "@created"; }
		}
		public override string PayloadParameter
		{
			get { return "@payload"; }
		}

		public override string LoadEvents
		{
			get { return MsSqlServerStatements.SelectEvents; }
		}
		public override string StoreEvents
		{
			get { return MsSqlServerStatements.InsertEvents; }
		}
		public override string StoreEvent
		{
			get { return MsSqlServerStatements.InsertEvent; }
		}

		public override string LoadSnapshot
		{
			get { return MsSqlServerStatements.SelectSnapshot; }
		}
		public override string StoreSnapshot
		{
			get { return MsSqlServerStatements.InsertSnapshot; }
		}

		public override bool IsConcurrencyException(DbException exception)
		{
			return exception.ErrorCode == PrimaryKeyViolation || exception.ErrorCode == UniqueIndexViolation;
		}
	}
}