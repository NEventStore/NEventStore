namespace EventStore.Core.SqlStorage.MySql
{
	using System;
	using System.Data;
	using System.Data.Common;

	public sealed class MySqlDialect : BaseDialect
	{
		private const string DuplicateEntryText = "Duplicate entry";
		private const string KeyViolationText = "for key";

		public MySqlDialect(IDbConnection connection, IDbTransaction transaction, Guid tenantId)
			: base(connection, transaction, tenantId)
		{
		}

		public override string Id
		{
			get { return "@id"; }
		}
		public override string InitialVersion
		{
			get { return "@initial_version"; }
		}
		public override string CurrentVersion
		{
			get { return "@current_version"; }
		}
		public override string Type
		{
			get { return "@type"; }
		}
		public override string Payload
		{
			get { return "@payload"; }
		}
		public override string SnapshotType
		{
			get { return "@snapshot_type"; }
		}
		public override string CommandId
		{
			get { return "@command_id"; }
		}
		public override string CommandPayload
		{
			get { return "@command_payload"; }
		}

		public override string SelectEvents
		{
			get { return MySqlStatements.SelectEvents; }
		}
		public override string SelectEventsForCommand
		{
			get { return MySqlStatements.SelectEventsForCommand; }
		}
		public override string SelectEventsForVersion
		{
			get { return MySqlStatements.SelectEventsForVersion; }
		}
		public override string InsertEvent
		{
			get { return MySqlStatements.InsertEvent; }
		}
		public override string InsertEvents
		{
			get { return MySqlStatements.InsertEvents; }
		}

		public override bool IsDuplicateKey(DbException exception)
		{
			if (exception == null)
				return false;

			var message = exception.Message;
			return message.Contains(DuplicateEntryText) && message.Contains(KeyViolationText);
		}
	}
}