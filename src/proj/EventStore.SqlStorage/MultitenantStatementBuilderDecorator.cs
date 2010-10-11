namespace EventStore.SqlStorage
{
	using System;
	using System.Data;

	public class MultitenantStatementBuilderDecorator : IBuildStatements
	{
		private const string DefaultParameterName = "tenant_id";
		private readonly IBuildStatements inner;
		private readonly string parameterName;
		private readonly Guid tenantId;

		public MultitenantStatementBuilderDecorator(IBuildStatements inner, Guid tenantId)
			: this(inner, tenantId, null)
		{
		}
		public MultitenantStatementBuilderDecorator(IBuildStatements inner, Guid tenantId, string parameterName)
		{
			this.inner = inner;
			this.tenantId = tenantId;
			this.parameterName = parameterName ?? DefaultParameterName;
		}

		public IAdaptSqlDialect Dialect
		{
			get { return this.inner.Dialect; }
		}
		public IDbCommand BuildLoadByIdQuery(Guid id, long maxStartingVersion)
		{
			return this.inner.BuildLoadByIdQuery(id, maxStartingVersion);
		}
		public IDbCommand BuildLoadByCommandIdQuery(Guid commandId)
		{
			return this.inner.BuildLoadByCommandIdQuery(commandId);
		}
		public IDbCommand BuildLoadStartingAfterQuery(Guid id, long version)
		{
			return this.inner.BuildLoadStartingAfterQuery(id, version);
		}
		public IDbCommand BuildSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer)
		{
			var command = this.inner.BuildSaveCommand(stream, serializer);
			command.AddParameter(this.TenantIdParam, this.tenantId);
			return command;
		}

		protected virtual string TenantIdParam
		{
			get { return this.Dialect.NormalizeParameterName(this.parameterName); }
		}
	}
}