namespace EventStore.SqlStorage.DynamicSql
{
	using System;
	using System.Data;
	using System.Text;

	public abstract class DynamicSqlStatementBuilder : IBuildStatements
	{
		private readonly CommandBuilder builder;
		private readonly IAdaptDynamicSqlDialect dialect;

		protected DynamicSqlStatementBuilder(CommandBuilder builder, IAdaptDynamicSqlDialect dialect)
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
			command.AddParameter(this.Id, id.ToNull());
			command.AddParameter(this.CurrentVersion, version.ToNull());
			return command;
		}

		public virtual IDbCommand BuildSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer)
		{
			var command = this.builder.Build(this.dialect.GetInsertEventsCommand);
			command.AddParameter(this.Id, stream.Id.ToNull());
			command.AddParameter(this.InitialVersion, stream.ExpectedVersion);
			command.AddParameter(this.CurrentVersion, stream.ExpectedVersion + stream.Events.Count);
			command.AddParameter(this.Type, stream.Type == null ? null : stream.Type.FullName);
			command.AddParameter(this.CommandId, stream.CommandId.ToNull());
			command.AddParameter(this.CommandPayload, serializer.Serialize(stream.Command));
			command.AddParameter(this.Payload, serializer.Serialize(stream.Snapshot));
			this.AppendEvents(command, stream, serializer);
			return command;
		}
		private void AppendEvents(IDbCommand command, UncommittedEventStream stream, ISerializeObjects serializer)
		{
			var commandText = new StringBuilder();
			var index = 0;

			foreach (var @event in stream.Events)
			{
				command.AddParameter(this.InitialVersion.Append(index), stream.ExpectedVersion + index + 1);
				command.AddParameter(this.Payload.Append(index), serializer.Serialize(@event));
				commandText.AppendWithFormat(this.dialect.GetInsertEventCommand, index++);
			}

			command.CommandText = command.CommandText.FormatWith(commandText);
		}

		protected virtual string Id
		{
			get { return this.dialect.NormalizeParameterName("id"); }
		}
		protected virtual string InitialVersion
		{
			get { return this.dialect.NormalizeParameterName("initial_version"); }
		}
		protected virtual string CurrentVersion
		{
			get { return this.dialect.NormalizeParameterName("current_version"); }
		}
		protected virtual string Type
		{
			get { return this.dialect.NormalizeParameterName("type"); }
		}
		protected virtual string Payload
		{
			get { return this.dialect.NormalizeParameterName("payload"); }
		}
		protected virtual string SnapshotType
		{
			get { return this.dialect.NormalizeParameterName("snapshot_type"); }
		}
		protected virtual string CommandId
		{
			get { return this.dialect.NormalizeParameterName("command_id"); }
		}
		protected virtual string CommandPayload
		{
			get { return this.dialect.NormalizeParameterName("command_payload"); }
		}
	}
}