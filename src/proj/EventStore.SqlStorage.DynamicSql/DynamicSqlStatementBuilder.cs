namespace EventStore.SqlStorage.DynamicSql
{
	using System;
	using System.Data;
	using System.Text;

	public class DynamicSqlStatementBuilder : IBuildStatements
	{
		private readonly CommandBuilder builder;
		private readonly IAdaptDynamicSqlDialect dialect;

		public DynamicSqlStatementBuilder(CommandBuilder builder, IAdaptDynamicSqlDialect dialect)
		{
			this.builder = builder;
			this.dialect = dialect;
		}

		public IAdaptSqlDialect Dialect
		{
			get { return this.dialect; }
		}

		public virtual IDbCommand BuildLoadByIdQuery(Guid id, long maxStartingVersion)
		{
			return this.BuildLoadQuery(this.dialect.GetSelectEventsQuery, id, maxStartingVersion);
		}
		public virtual IDbCommand BuildLoadByCommandIdQuery(Guid commandId)
		{
			return this.BuildLoadQuery(this.dialect.GetSelectEventsForCommandQuery, commandId, 0);
		}
		public virtual IDbCommand BuildLoadStartingAfterQuery(Guid id, long version)
		{
			return this.BuildLoadQuery(this.dialect.GetSelectEventsForVersionQuery, id, version);
		}
		private IDbCommand BuildLoadQuery(string commandText, Guid id, long version)
		{
			var command = this.builder.Build(commandText);
			command.AddParameter(this.IdParam, id.ToNull());
			command.AddParameter(this.CurrentVersionParam, version.ToNull());
			return command;
		}

		public virtual IDbCommand BuildSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer)
		{
			var command = this.builder.Build(this.dialect.GetInsertEventsCommand);
			command.AddParameter(this.IdParam, stream.Id.ToNull());
			command.AddParameter(this.InitialVersionParam, stream.ExpectedVersion);
			command.AddParameter(this.CurrentVersionParam, stream.ExpectedVersion + stream.Events.Count);
			command.AddParameter(this.TypeParam, stream.Type == null ? null : stream.Type.FullName);
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
				command.AddParameter(this.InitialVersionParam.Append(index), stream.ExpectedVersion + index + 1);
				command.AddParameter(this.PayloadParam.Append(index), serializer.Serialize(@event));
				commandText.AppendWithFormat(this.dialect.GetInsertEventCommand, index++);
			}

			command.CommandText = command.CommandText.FormatWith(commandText);
		}

		protected virtual string IdParam
		{
			get { return this.dialect.NormalizeParameterName("id"); }
		}
		protected virtual string InitialVersionParam
		{
			get { return this.dialect.NormalizeParameterName("initial_version"); }
		}
		protected virtual string CurrentVersionParam
		{
			get { return this.dialect.NormalizeParameterName("current_version"); }
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