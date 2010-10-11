namespace EventStore.Core.SqlStorage
{
	using System;
	using System.Data;
	using System.Data.Common;
	using System.Text;

	public abstract class DynamicSqlStatementPreparer : IPrepareStatements
	{
		private const string ConstraintViolation = "constraint";
		private readonly CommandBuilder builder;

		protected DynamicSqlStatementPreparer(CommandBuilder builder)
		{
			this.builder = builder;
		}

		public virtual IDbCommand PrepareLoadByIdQuery(Guid id, long maxStartingVersion)
		{
			return this.PrepareLoadQuery(this.SelectEvents, id, maxStartingVersion);
		}
		public virtual IDbCommand PrepareLoadByCommandIdQuery(Guid commandId)
		{
			return this.PrepareLoadQuery(this.SelectEventsForCommand, commandId, 0);
		}
		public virtual IDbCommand PrepareLoadStartingAfterQuery(Guid id, long version)
		{
			return this.PrepareLoadQuery(this.SelectEventsForVersion, id, version);
		}
		private IDbCommand PrepareLoadQuery(string commandText, Guid id, long version)
		{
			var command = this.builder.Build(commandText);
			command.AddParameter(this.Id, id.ToNull());
			command.AddParameter(this.CurrentVersion, version.ToNull());
			return command;
		}

		public virtual IDbCommand PrepareSaveCommand(UncommittedEventStream stream, ISerializeObjects serializer)
		{
			var command = this.builder.Build(this.InsertEvents);
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
				commandText.AppendWithFormat(this.InsertEvent, index++);
			}

			command.CommandText = command.CommandText.FormatWith(commandText);
		}

		public virtual bool IsConstraintViolation(DbException exception)
		{
			return exception.Message.ToLowerInvariant().Contains(ConstraintViolation);
		}
		public abstract bool IsDuplicateKey(DbException exception);

		public virtual string NormalizeParameterName(string parameterName)
		{
			return "@" + parameterName;
		}

		protected virtual string Id
		{
			get { return "@id"; }
		}
		protected virtual string InitialVersion
		{
			get { return "@initial_version"; }
		}
		protected virtual string CurrentVersion
		{
			get { return "@current_version"; }
		}
		protected virtual string Type
		{
			get { return "@type"; }
		}
		protected virtual string Payload
		{
			get { return "@payload"; }
		}
		protected virtual string SnapshotType
		{
			get { return "@snapshot_type"; }
		}
		protected virtual string CommandId
		{
			get { return "@command_id"; }
		}
		protected virtual string CommandPayload
		{
			get { return "@command_payload"; }
		}

		protected abstract string SelectEvents { get; }
		protected abstract string SelectEventsForCommand { get; }
		protected abstract string SelectEventsForVersion { get; }
		protected abstract string InsertEvents { get; }
		protected abstract string InsertEvent { get; }
	}
}