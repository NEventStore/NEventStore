namespace EventStore.SqlStorage.DynamicSql
{
	using System;
	using System.Data;
	using System.Data.Common;

	public class MultitenantStatementBuilder : IBuildStatements
	{
		private const string DefaultParameterName = "@tenant_id";

		private readonly IBuildStatements statementBuilder;
		private readonly Guid tenantId;
		private readonly string parameterName;

		public MultitenantStatementBuilder(IBuildStatements statementBuilder, Guid tenantId)
			: this(statementBuilder, tenantId, null)
		{
		}
		public MultitenantStatementBuilder(IBuildStatements statementBuilder, Guid tenantId, string parameterName)
		{
			this.statementBuilder = statementBuilder;
			this.tenantId = tenantId;
			this.parameterName = parameterName ?? DefaultParameterName;
		}

		public IDbCommand BuildLoadByIdQuery(Guid id, long maxStartingVersion)
		{
			return this.statementBuilder.BuildLoadByIdQuery(id, maxStartingVersion);
		}
		public IDbCommand BuildLoadByCommandIdQuery(Guid commandId)
		{
			return this.statementBuilder.BuildLoadByCommandIdQuery(commandId);
		}
		public IDbCommand BuildLoadStartingAfterQuery(Guid id, long version)
		{
			return this.statementBuilder.BuildLoadStartingAfterQuery(id, version);
		}
		public IDbCommand BuildSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer)
		{
			var command = this.statementBuilder.BuildSaveCommand(stream, serializer);
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