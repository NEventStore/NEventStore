namespace EventStore.Core.SqlStorage
{
	using System;
	using System.Data;
	using System.Data.Common;

	public abstract class BaseDialect : ISqlDialect
	{
		private const string ConstraintViolation = "constraint";
		private readonly IDbConnection connection;
		private readonly IDbTransaction transaction;
		private readonly Guid tenantId;

		protected BaseDialect(IDbConnection connection, IDbTransaction transaction, Guid tenantId)
		{
			this.connection = connection;
			this.transaction = transaction;
			this.tenantId = tenantId; // shared schema: http://msdn.microsoft.com/en-us/library/aa479086.aspx
		}

		public virtual string Id
		{
			get { return "@id"; }
		}
		public virtual string TenantId
		{
			get { return "@tenant_id"; }
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
		public abstract string SelectEventsForCommand { get; }
		public abstract string SelectEventsForVersion { get; }
		public abstract string InsertEvents { get; }
		public abstract string InsertEvent { get; }

		public virtual IDbCommand CreateCommand(string commandText)
		{
			var command = this.connection.CreateCommand();
			command.CommandText = commandText;
			command.Transaction = this.transaction;
			command.AddParameter(this.TenantId, this.tenantId);

			return command;
		}
		public virtual bool IsConstraintViolation(DbException exception)
		{
			return exception.Message.ToLowerInvariant().Contains(ConstraintViolation);
		}

		public abstract bool IsDuplicateKey(DbException exception);
	}
}