namespace EventStore.Core.Sql
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Text;

	public class SqlEventStore : IStoreEvents
	{
		private const int SerializedDataIndex = 0;
		private const int VersionIndex = 1;
		private readonly IDictionary<Guid, long> versions = new Dictionary<Guid, long>();
		private readonly IDbConnection connection;
		private readonly SqlDialect dialect;
		private readonly ISerialize serializer;

		public SqlEventStore(IDbConnection connection, SqlDialect dialect, ISerialize serializer)
		{
			this.connection = connection;
			this.dialect = dialect;
			this.serializer = serializer;
		}

		public CommittedEventStream Read(Guid id)
		{
			return this.Read(id, 0, this.dialect.SelectEvents);
		}
		public CommittedEventStream ReadStartingFrom(Guid id, long version)
		{
			return this.Read(id, version, this.dialect.SelectEventsWhere);
		}
		private CommittedEventStream Read(Guid id, long version, string queryStatement)
		{
			using (var command = this.connection.CreateCommand())
			{
				command.CommandText = queryStatement;
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
				events.Add(this.serializer.Deserialize<object>(reader[SerializedDataIndex] as byte[]));

			if (reader.NextResult() && reader.Read())
			{
				snapshot = this.serializer.Deserialize<object>(reader[SerializedDataIndex] as byte[]);
				version = (long)reader[VersionIndex];
			}

			this.versions[id] = version + events.Count;
			return new CommittedEventStream(id, version + events.Count, events, snapshot);
		}

		public void Write(UncommittedEventStream stream)
		{
			using (var command = this.connection.CreateCommand())
			{
				long initialVersion;
				this.versions.TryGetValue(stream.Id, out initialVersion);
				this.versions[stream.Id] = initialVersion + stream.Events.Count;

				command.AddParameter(this.dialect.Id, stream.Id);
				command.AddParameter(this.dialect.InitialVersion, initialVersion);
				command.AddParameter(this.dialect.CurrentVersion, initialVersion + stream.Events.Count);
				command.AddParameter(this.dialect.Type, stream.Type == null ? null : stream.Type.FullName);
				command.AddParameter(this.dialect.Payload, this.serializer.Serialize(stream.Snapshot));

				this.WriteEventsToCommand(command, stream, initialVersion);
				this.WrapOnFailure(() => command.ExecuteNonQuery());
			}
		}
		private void WriteEventsToCommand(IDbCommand command, UncommittedEventStream stream, long initialVersion)
		{
			var eventInsertStatements = new StringBuilder();
			var index = 0;

			foreach (var @event in stream.Events)
			{
				command.AddParameter(this.dialect.InitialVersion.Append(index), initialVersion + index + 1);
				command.AddParameter(this.dialect.Payload.Append(index), this.serializer.Serialize(@event));
				eventInsertStatements.AppendWithFormat(this.dialect.InsertEvent, index++);
			}

			command.CommandText = this.dialect.InsertEvents.FormatWith(eventInsertStatements);
		}

		private TResult WrapOnFailure<TResult>(Func<TResult> action)
		{
			try
			{
				return action();
			}
			catch (DbException exception)
			{
				if (this.dialect.IsConcurrencyException(exception))
					throw new ConcurrencyException(exception.Message, exception);

				throw new EventStoreException(exception.Message, exception);
			}
		}
	}
}