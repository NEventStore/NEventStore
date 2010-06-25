namespace EventStore.Core.Sql
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Text;

	public class SqlEventStore<T> : IStoreEvents<T>
	{
		private const int SerializedDataIndex = 0;
		private const int VersionIndex = 1;
		private readonly IDictionary<Guid, int> versions = new Dictionary<Guid, int>();
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

		public EventStream<T> Read(Guid id)
		{
			return Read(id, 0, this.dialect.SelectEvents);
		}
		public EventStream<T> ReadFrom(Guid id, int startingVersion)
		{
			return Read(id, startingVersion, this.dialect.SelectEventsWhere);
		}
		private EventStream<T> Read(Guid id, int version, string queryStatement)
		{
			using (var command = this.connection.CreateCommand())
			{
				command.CommandText = queryStatement;
				command.AddParameter(this.dialect.Id, id);
				command.AddParameter(this.dialect.Version, version);
				using (var reader = this.WrapOnFailure(() => command.ExecuteReader()))
					return BuildStream(id, version, reader);
			}
		}
		private EventStream<T> BuildStream(Guid id, int version, IDataReader reader)
		{
			ICollection<T> events = new LinkedList<T>();
			var stream = new EventStream<T>
			{
				Id = id,
				Events = events
			};

			while (reader.Read())
				events.Add(this.serializer.Deserialize<T>(reader[SerializedDataIndex] as byte[]));

			if (reader.NextResult() && reader.Read())
			{
				stream.Snapshot = this.serializer.Deserialize<object>(reader[SerializedDataIndex] as byte[]);
				version = (int)reader[VersionIndex];
			}

			this.versions[id] = stream.Version = version + events.Count;
			return stream;
		}

		public void Write(EventStream<T> stream)
		{
			using (var command = this.connection.CreateCommand())
			{
				int versionWhenLoaded;
				this.versions.TryGetValue(stream.Id, out versionWhenLoaded);

				command.AddParameter(this.dialect.Id, stream.Id);
				command.AddParameter(this.dialect.Version, stream.Version);
				command.AddParameter(this.dialect.Type, stream.Type.FullName);
				command.AddParameter(this.dialect.Created, this.now());
				command.AddParameter(this.dialect.MomentoType, stream.Snapshot.GetTypeName());
				command.AddParameter(this.dialect.Payload, this.serializer.Serialize(stream.Snapshot));

				this.WriteEventsToCommand(command, stream);
				this.WrapOnFailure(() => command.ExecuteNonQuery());
				this.versions[stream.Id] = stream.Version = versionWhenLoaded + stream.Events.Count;
			}
		}
		private void WriteEventsToCommand(IDbCommand command, EventStream<T> stream)
		{
			var eventInsertStatements = new StringBuilder();
			var index = 0;

			foreach (var @event in stream.Events)
			{
				command.AddParameter(this.dialect.Type.Append(index), @event.GetTypeName());
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