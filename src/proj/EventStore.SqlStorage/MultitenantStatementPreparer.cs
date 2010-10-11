namespace EventStore.SqlStorage
{
	using System;
	using System.Data;
	using System.Data.Common;

	public class MultitenantStatementPreparer : IPrepareStatements
	{
		private const string DefaultParameterName = "@tenant_id";

		private readonly IPrepareStatements statementBuilder;
		private readonly Guid tenantId;
		private readonly string parameterName;

		public MultitenantStatementPreparer(IPrepareStatements statementBuilder, Guid tenantId)
			: this(statementBuilder, tenantId, null)
		{
		}
		public MultitenantStatementPreparer(IPrepareStatements statementBuilder, Guid tenantId, string parameterName)
		{
			this.statementBuilder = statementBuilder;
			this.tenantId = tenantId;
			this.parameterName = parameterName ?? DefaultParameterName;
		}

		public IDbCommand PrepareLoadByIdQuery(Guid id, long maxStartingVersion)
		{
			return this.statementBuilder.PrepareLoadByIdQuery(id, maxStartingVersion);
		}
		public IDbCommand PrepareLoadByCommandIdQuery(Guid commandId)
		{
			return this.statementBuilder.PrepareLoadByCommandIdQuery(commandId);
		}
		public IDbCommand PrepareLoadStartingAfterQuery(Guid id, long version)
		{
			return this.statementBuilder.PrepareLoadStartingAfterQuery(id, version);
		}
		public IDbCommand PrepareSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer)
		{
			var command = this.statementBuilder.PrepareSaveCommand(stream, serializer);
			command.AddParameter(this.parameterName, this.tenantId);
			return command;
		}
		public bool IsConstraintViolation(DbException exception)
		{
			return this.statementBuilder.IsConstraintViolation(exception);
		}
		public bool IsDuplicateKey(DbException exception)
		{
			return this.statementBuilder.IsDuplicateKey(exception);
		}
	}
}