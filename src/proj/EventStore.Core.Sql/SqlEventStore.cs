namespace EventStore.Core.Sql
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Text;
	using System.Globalization;

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
			where T : class
		{
			using (var command = this.CreateCommand(this.dialect.LoadEvents, id))
			{
				command.AddWithValue(this.dialect.VersionParameter, startingVersion);
				using (var reader = command.ExecuteReader())
					while (reader.Read())
						yield return this.serializer.Deserialize<T>(reader[0] as byte[]);
			}
		}
		private IDbCommand CreateCommand(string commandText, Guid id)
		{
			var command = this.connection.CreateCommand();
			command.CommandText = commandText;
			command.AddWithValue(this.dialect.IdParameter, id);
			return command;
		}

		public int StoreEvents<T>(Guid id, IEnumerable<T> events)
			where T : class
		{
			using (var command = this.CreateCommand(this.dialect.StoreEvents, id))
			{
				command.AddWithValue(this.dialect.VersionParameter, 0);
				((IDataParameter)command.Parameters[1]).Direction = ParameterDirection.Output;

				command.AddWithValue(this.dialect.RuntimeTypeParameter, string.Empty); // Aggregate Type: TODO
				command.AddWithValue(this.dialect.CreatedParameter, DateTime.UtcNow);

				this.AddEventsToCommand(command, events);

				return (int)command.ExecuteScalar();
			}
		}
		private void AddEventsToCommand<T>(IDbCommand command, IEnumerable<T> events)
		{
			var insertText = new StringBuilder();
			var index = 0;

			foreach (var @event in events)
			{
				command.AddWithValue(
					GetParameterName(this.dialect.RuntimeTypeParameter, index),
					@event.GetType().FullName);

				command.AddWithValue(
					GetParameterName(this.dialect.PayloadParameter, index),
					this.serializer.Serialize(@event));

				insertText.AppendFormat(CultureInfo.InvariantCulture, this.dialect.StoreEvent, index++);
			}

			command.CommandText = string.Format(
				CultureInfo.InvariantCulture, command.CommandText, insertText);

		}
		private static string GetParameterName(string parameterNameFormat, int index)
		{
			return string.Format(CultureInfo.InvariantCulture, parameterNameFormat, index);
		}

		public T LoadSnapshot<T>(Guid id)
			where T : class
		{
			using (var command = this.CreateCommand(this.dialect.LoadSnapshot, id))
			{
				var selected = command.ExecuteScalar();
				return this.serializer.Deserialize<T>(selected == DBNull.Value ? null : selected as byte[]);
			}
		}

		public void StoreSnapshot<T>(Guid id, int version, T snapshot)
			where T : class
		{
			using (var command = this.CreateCommand(this.dialect.StoreSnapshot, id))
			{
				command.AddWithValue(this.dialect.VersionParameter, version);
				command.AddWithValue(this.dialect.RuntimeTypeParameter, snapshot.GetType().FullName);
				command.AddWithValue(this.dialect.CreatedParameter, DateTime.UtcNow);
				command.AddWithValue(this.dialect.PayloadParameter, this.serializer.Serialize(snapshot));
				command.ExecuteNonQuery();
			}
		}
	}
}