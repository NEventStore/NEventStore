namespace EventStore.Core.Sql
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Text;

	public class SqlStorageEngine : IStorageEngine
	{
		private const int SerializedDataColumnIndex = 0;
		private const int VersionColumnIndex = 1;
		private readonly SqlDialect dialect;
		private readonly ISerialize serializer;

		public SqlStorageEngine(SqlDialect dialect, ISerialize serializer)
		{
			this.dialect = dialect;
			this.serializer = serializer;
		}

		public CommittedEventStream LoadById(Guid id)
		{
			return this.Read(id, 0, this.dialect.SelectEvents);
		}
		public ICollection LoadStartingAfter(Guid id, long version)
		{
			if (id == Guid.Empty)
				return new object[0];

			return this.Read(id, version, this.dialect.SelectEventsForVersion).Events;
		}
		public ICollection LoadByCommandId(Guid commandId)
		{
			if (commandId == Guid.Empty)
				return new object[0];

			return this.Read(commandId, 0, this.dialect.SelectEventsForCommand).Events;
		}
		private CommittedEventStream Read(Guid id, long version, string queryStatement)
		{
			using (var command = this.dialect.CreateCommand(queryStatement))
			{
				command.AddParameter(this.dialect.Id, id);
				command.AddParameter(this.dialect.CurrentVersion, version);
				using (var reader = this.WrapOnFailure(() => command.ExecuteReader()))
					return this.BuildStream(id, version, reader);
			}
		}
		private CommittedEventStream BuildStream(Guid id, long version, IDataReader reader)
		{
			ICollection<object> events = new LinkedList<object>();
			object snapshot = null;

			while (reader.Read())
				events.Add(this.serializer.Deserialize<object>(reader[SerializedDataColumnIndex] as byte[]));

			if (reader.NextResult() && reader.Read())
			{
				snapshot = this.serializer.Deserialize<object>(reader[SerializedDataColumnIndex] as byte[]);
				version = (long)reader[VersionColumnIndex];
			}

			return new CommittedEventStream(id, version + events.Count, (ICollection)events, snapshot);
		}

		public void Save(UncommittedEventStream stream, long initialVersion)
		{
			using (var command = this.dialect.CreateCommand(this.dialect.InsertEvents))
			{
				command.AddParameter(this.dialect.Id, stream.Id);
				command.AddParameter(this.dialect.InitialVersion, initialVersion);
				command.AddParameter(this.dialect.CurrentVersion, initialVersion + stream.Events.Count);
				command.AddParameter(this.dialect.Type, stream.Type == null ? null : stream.Type.FullName);
				command.AddParameter(this.dialect.CommandId, stream.CommandId.ToNull());
				command.AddParameter(this.dialect.CommandPayload, this.serializer.Serialize(stream.Command));
				command.AddParameter(this.dialect.Payload, this.serializer.Serialize(stream.Snapshot));

				this.AddEventsToDbCommand(command, stream, initialVersion);
				this.WrapOnFailure(() => command.ExecuteNonQuery());
			}
		}
		private void AddEventsToDbCommand(IDbCommand command, UncommittedEventStream stream, long version)
		{
			var eventInsertStatements = new StringBuilder();
			var index = 0;

			foreach (var @event in stream.Events)
			{
				command.AddParameter(this.dialect.InitialVersion.Append(index), version + index + 1);
				command.AddParameter(this.dialect.Payload.Append(index), this.serializer.Serialize(@event));
				eventInsertStatements.AppendWithFormat(this.dialect.InsertEvent, index++);
			}

			command.CommandText = command.CommandText.FormatWith(eventInsertStatements);
		}

		private TResult WrapOnFailure<TResult>(Func<TResult> action)
		{
			try
			{
				return action();
			}
			catch (DbException exception)
			{
				if (this.dialect.IsDuplicateKey(exception))
					throw new DuplicateKeyException(exception.Message, exception);

				throw new StorageEngineException(exception.Message, exception);
			}
		}
	}
}