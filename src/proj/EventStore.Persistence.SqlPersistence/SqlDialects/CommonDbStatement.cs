namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Data;

	public class CommonDbStatement : IDbStatement
	{
		protected IDictionary<string, object> Parameters { get; private set; }
		private readonly IDbConnection connection;
		private readonly IDbTransaction transaction;

		public CommonDbStatement(IDbConnection connection, IDbTransaction transaction)
		{
			this.Parameters = new Dictionary<string, object>();
			this.connection = connection;
			this.transaction = transaction;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// no op
		}

		public virtual void AddParameter(string name, object value)
		{
			this.Parameters[name] = value;
		}

		public virtual int ExecuteWithSuppression(string commandText)
		{
			try
			{
				return this.Execute(commandText);
			}
			catch (Exception)
			{
				return 0;
			}
		}
		public virtual int Execute(string commandText)
		{
			return this.ExecuteNonQuery(commandText);
		}
		protected int ExecuteNonQuery(string commandText)
		{
			try
			{
				using (var command = this.BuildCommand(commandText))
					return command.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				if (this.IsDuplicate(e))
					throw new DuplicateCommitException(e.Message, e);

				throw;
			}
		}

		protected virtual bool IsDuplicate(Exception exception)
		{
			var message = exception.Message.ToUpperInvariant();
			return message.Contains("DUPLICATE") || message.Contains("UNIQUE");
		}

		public virtual IEnumerable<T> ExecuteWithQuery<T>(string queryText, Func<IDataRecord, T> select)
		{
			using (var query = this.BuildCommand(queryText))
			using (var reader = query.ExecuteReader())
			{
				ICollection<T> items = new LinkedList<T>();

				while (reader.Read())
					items.Add(select(reader));

				return items;
			}
		}

		private IDbCommand BuildCommand(string statement)
		{
			var command = this.connection.CreateCommand();
			command.Transaction = this.transaction;
			command.CommandText = statement;

			this.BuildParameters(command);

			return command;
		}
		protected virtual void BuildParameters(IDbCommand command)
		{
			foreach (var item in this.Parameters)
				this.BuildParameter(command, item.Key, item.Value);
		}
		protected virtual void BuildParameter(IDbCommand command, string name, object value)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = name;
			this.SetParameterValue(parameter, value, null);
			command.Parameters.Add(parameter);
		}
		protected virtual void SetParameterValue(IDataParameter param, object value, DbType? type)
		{
			param.Value = value ?? DBNull.Value;
			param.DbType = type ?? param.DbType;
		}
	}
}