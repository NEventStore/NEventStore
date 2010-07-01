namespace EventStore.Core.Sql
{
	using System.Data;
	using System.Data.Common;

	public abstract class SqlDialect
	{
		private readonly IDbConnection connection;
		private readonly IDbTransaction transaction;

		protected SqlDialect(IDbConnection connection, IDbTransaction transaction)
		{
			this.connection = connection;
			this.transaction = transaction;
		}

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
		public virtual string CommandId
		{
			get { return "@command_id"; }
		}
		public virtual string CommandPayload
		{
			get { return "@command_payload"; }
		}

		public abstract string SelectEvents { get; }
		public virtual string SelectEventsForCommand
		{
			get { return SqlStatements.SelectEventsForCommand; }
		}
		public virtual string SelectEventsForVersion
		{
			get { return SqlStatements.SelectEventsForVersion; }
		}
		public abstract string InsertEvents { get; }
		public abstract string InsertEvent { get; }

		public virtual IDbCommand CreateCommand(string commandText)
		{
			var command = this.connection.CreateCommand();
			command.CommandText = commandText;
			command.Transaction = this.transaction;
			return command;
		}

		public abstract bool IsDuplicateKey(DbException exception);
	}
}