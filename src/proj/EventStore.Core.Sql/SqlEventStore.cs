namespace EventStore.Core.Sql
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using System.Text;

	public class SqlEventStore : IStoreEvents
	{
		private readonly IDbConnection connection;
		private readonly SqlDialect dialect;
		private readonly ISerialize serializer;

		public SqlEventStore(IDbConnection connection, SqlDialect dialect, ISerialize serializer)
		{
			this.connection = connection;
			this.dialect = dialect;
			this.serializer = serializer;
		}

		public IEnumerable<T> LoadEvents<T>(Guid id, int startingVersion)
		{
			using (var command = this.connection.CreateCommand())
			{
				command.CommandText = this.dialect.LoadEvents;
				command.AddWithValue(this.dialect.IdParameter, id);
				command.AddWithValue(this.dialect.VersionParameter, startingVersion);
				using (var reader = command.ExecuteReader())
					while (reader.Read())
						yield return this.serializer.Deserialize<T>(reader[0] as byte[]);
			}
		}

		public int StoreEvents<T>(Guid id, Type aggregate, IEnumerable<T> events)
		{
			using (var command = this.connection.CreateCommand())
			{
				command.CommandText = this.dialect.StoreEvents;
				command.AddWithValue(this.dialect.IdParameter, id);
				command.AddWithValue(this.dialect.RuntimeTypeParameter, aggregate.FullName);
				command.AddWithValue(this.dialect.CreatedParameter, DateTime.UtcNow);
				var version = command.AddWithValue(this.dialect.VersionParameter, 0, ParameterDirection.Output);
				this.AddEventsToCommand(command, events);
				command.ExecuteNonQuery();

				return (int)version.Value;
			}
		}
		private void AddEventsToCommand(IDbCommand command, IEnumerable events)
		{
			var eventInsertStatements = new StringBuilder();
			var index = 0;

			foreach (var @event in events)
			{
				command.AddWithValue(
					this.dialect.RuntimeTypeParameter + index.ToString(CultureInfo.InvariantCulture),
					@event.GetType().FullName);

				command.AddWithValue(
					this.dialect.PayloadParameter + index.ToString(CultureInfo.InvariantCulture),
					this.serializer.Serialize(@event));

				eventInsertStatements.AppendFormat(
					CultureInfo.InvariantCulture, this.dialect.StoreEvent, index++);
			}

			command.CommandText = string.Format(
				CultureInfo.InvariantCulture, command.CommandText, eventInsertStatements);
		}

		public T LoadSnapshot<T>(Guid id)
		{
			using (var command = this.connection.CreateCommand())
			{
				command.CommandText = this.dialect.LoadSnapshot;
				command.AddWithValue(this.dialect.IdParameter, id);
				var selected = command.ExecuteScalar();
				return this.serializer.Deserialize<T>(selected == DBNull.Value ? null : selected as byte[]);
			}
		}

		public void StoreSnapshot<T>(Guid id, int version, T snapshot)
		{
			using (var command = this.connection.CreateCommand())
			{
				command.CommandText = this.dialect.StoreSnapshot;
				command.AddWithValue(this.dialect.IdParameter, id);
				command.AddWithValue(this.dialect.VersionParameter, version);
				command.AddWithValue(this.dialect.RuntimeTypeParameter, snapshot.GetType().FullName);
				command.AddWithValue(this.dialect.CreatedParameter, DateTime.UtcNow);
				command.AddWithValue(this.dialect.PayloadParameter, this.serializer.Serialize(snapshot));
				command.ExecuteNonQuery();
			}
		}
	}
}