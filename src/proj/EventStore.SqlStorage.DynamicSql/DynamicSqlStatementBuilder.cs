namespace EventStore.SqlStorage.DynamicSql
{
	using System;
	using System.Data;
	using System.Text;

	public class DynamicSqlStatementBuilder : IBuildStatements
	{
		private readonly CommandBuilder builder;
		private readonly IAdaptDynamicSqlDialect dialect;
		private readonly Guid tenantId;

		public DynamicSqlStatementBuilder(CommandBuilder builder, IAdaptDynamicSqlDialect dialect)
			: this(builder, dialect, Guid.Empty)
		{
		}
		public DynamicSqlStatementBuilder(CommandBuilder builder, IAdaptDynamicSqlDialect dialect, Guid tenantId)
		{
			this.builder = builder;
			this.dialect = dialect;
			this.tenantId = tenantId;
		}

		public IAdaptSqlDialect Dialect
		{
			get { return this.dialect; }
		}

		public virtual IDbCommand BuildLoadByIdQuery(Guid id, long maxStartingVersion)
		{
			var query = this.builder.Build(this.dialect.GetSelectEventsQuery);
			query.AddParameter(this.IdParam, id);
			query.AddParameter(this.CommittedVersionParam, maxStartingVersion);
			return query;
		}
		public virtual IDbCommand BuildLoadByCommandIdQuery(Guid commandId)
		{
			var query = this.builder.Build(this.dialect.GetSelectEventsForCommandQuery);
			query.AddParameter(this.IdParam, commandId);
			return query;
		}
		public virtual IDbCommand BuildLoadStartingAfterQuery(Guid id, long version)
		{
			var query = this.builder.Build(this.dialect.GetSelectEventsSinceVersionQuery);
			query.AddParameter(this.IdParam, id);
			query.AddParameter(this.CommittedVersionParam, version);
			query.AddParameter(this.TenantIdParam, this.tenantId.ToNull());
			return query;
		}

		public virtual IDbCommand BuildSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer)
		{
			var command = this.builder.Build(this.dialect.GetInsertEventsCommand);
			command.AddParameter(this.IdParam, stream.Id);
			command.AddParameter(this.TenantIdParam, this.tenantId.ToNull());
			command.AddParameter(this.CommittedVersionParam, stream.CommittedVersion);
			command.AddParameter(this.NewVersionParam, stream.CommittedVersion + stream.Events.Count);
			command.AddParameter(this.TypeParam, stream.Type == null ? string.Empty : stream.Type.FullName);
			command.AddParameter(this.CommandIdParam, stream.CommandId.ToNull());
			command.AddParameter(this.CommandPayloadParam, serializer.Serialize(stream.Command));
			command.AddParameter(this.PayloadParam, serializer.Serialize(stream.Snapshot));
			this.AppendEvents(command, stream, serializer);
			return command;
		}
		private void AppendEvents(IDbCommand command, UncommittedEventStream stream, ISerializeObjects serializer)
		{
			var commandText = new StringBuilder();
			var index = 0;

			foreach (var @event in stream.Events)
			{
				command.AddParameter(this.CommittedVersionParam.Append(index), stream.CommittedVersion + index + 1);
				command.AddParameter(this.PayloadParam.Append(index), serializer.Serialize(@event));
				commandText.AppendWithFormat(this.dialect.GetInsertEventCommand, index++);
			}

			command.CommandText = command.CommandText.FormatWith(commandText);
		}

		protected virtual string IdParam
		{
			get { return this.dialect.NormalizeParameterName("id"); }
		}
		protected virtual string TenantIdParam
		{
			get { return this.dialect.NormalizeParameterName("tenant_id"); }
		}
		protected virtual string CommittedVersionParam
		{
			get { return this.dialect.NormalizeParameterName("committed_version"); }
		}
		protected virtual string NewVersionParam
		{
			get { return this.dialect.NormalizeParameterName("new_version"); }
		}
		protected virtual string TypeParam
		{
			get { return this.dialect.NormalizeParameterName("type"); }
		}
		protected virtual string PayloadParam
		{
			get { return this.dialect.NormalizeParameterName("payload"); }
		}
		protected virtual string SnapshotTypeParam
		{
			get { return this.dialect.NormalizeParameterName("snapshot_type"); }
		}
		protected virtual string CommandIdParam
		{
			get { return this.dialect.NormalizeParameterName("command_id"); }
		}
		protected virtual string CommandPayloadParam
		{
			get { return this.dialect.NormalizeParameterName("command_payload"); }
		}
	}
}