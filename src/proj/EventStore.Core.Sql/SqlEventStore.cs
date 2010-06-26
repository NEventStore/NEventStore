namespace EventStore.Core.Sql
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Text;

	public class SqlEventStore : IStoreEvents
	{
		private const int SerializedDataIndex = 0;
		private const int TypeIndex = 1;
		private const int VersionIndex = 2;
		private readonly IDictionary<Guid, long> versions = new Dictionary<Guid, long>();
		private readonly IDbConnection connection;
		private readonly SqlDialect dialect;
		private readonly ISerialize serializer;
		private readonly Func<DateTime> now;

		public SqlEventStore(
			IDbConnection connection, SqlDialect dialect, ISerialize serializer, Func<DateTime> now)
		{
			this.connection = connection;
			this.dialect = dialect;
			this.serializer = serializer;
			this.now = now;
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
				command.AddParameter(this.dialect.Version, version);
				using (var reader = this.WrapOnFailure(() => command.ExecuteReader()))
					return this.BuildStream(id, version, reader);
			}
		}
		private CommittedEventStream BuildStream(Guid id, long version, IDataReader reader)
		{
			ICollection<object> events = new LinkedList<object>();
			var stream = new CommittedEventStream
			{
				Id = id,
				Events = (ICollection)events
			};

			while (reader.Read())
				events.Add(this.serializer.Deserialize<object>(reader[SerializedDataIndex] as byte[]));

			if (reader.NextResult() && reader.Read())
			{
				stream.Snapshot = this.serializer.Deserialize<object>(reader[SerializedDataIndex] as byte[]);
				version = (long)reader[VersionIndex];
			}

			this.versions[id] = stream.Version = version + events.Count;
			return stream;
		}

		public void Write(UncommittedEventStream stream)
		{
			using (var command = this.connection.CreateCommand())
			{
				long versionWhenLoaded;
				this.versions.TryGetValue(stream.Id, out versionWhenLoaded);
				this.versions[stream.Id] = versionWhenLoaded + stream.Events.Count;

				command.AddParameter(this.dialect.Id, stream.Id);
				// TODO: SQLite doesn't support modifying parameters, we therefore must supply all of them
				command.AddParameter(this.dialect.Version, versionWhenLoaded);
				command.AddParameter(this.dialect.Type, stream.Type.FullName);
				command.AddParameter(this.dialect.Created, this.now());
				command.AddParameter(this.dialect.SnapshotType, stream.Snapshot.GetTypeName());
				command.AddParameter(this.dialect.Payload, this.serializer.Serialize(stream.Snapshot));

				this.WriteEventsToCommand(command, stream);
				this.WrapOnFailure(() => command.ExecuteNonQuery());
			}
		}
		private void WriteEventsToCommand(IDbCommand command, UncommittedEventStream stream)
		{
			var eventInsertStatements = new StringBuilder();
			var index = 0;

			foreach (var @event in stream.Events)
			{
				command.AddParameter(this.dialect.Type.Append(index), @event.GetTypeName());
				command.AddParameter(this.dialect.Payload.Append(index), this.serializer.Serialize(@event));
				// TODO: SQLite doesn't support modifying parameters, we therefore must supply all of them
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