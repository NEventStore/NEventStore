namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;

	public class CommonDbStatement : IDbStatement
	{
		protected IDictionary<string, object> Parameters { get; private set; }
		private readonly ISqlDialect dialect;
		private readonly IDbTransaction transaction;
		private readonly IDbConnection connection;
		private readonly IDisposable[] resources;

		public CommonDbStatement(
			ISqlDialect dialect,
			IDbTransaction transaction,
			IDbConnection connection,
			params IDisposable[] resources)
		{
			this.Parameters = new Dictionary<string, object>();

			this.dialect = dialect;
			this.connection = connection;
			this.transaction = transaction;
			this.resources = resources ?? new IDisposable[0];
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (this.transaction != null)
				this.transaction.Dispose();

			if (this.connection != null)
				this.connection.Dispose();

			foreach (var resource in this.resources.Reverse().Where(resource => resource != null))
				resource.Dispose(); // dispose from the inside out
		}

		public virtual void AddParameter(string name, object value)
		{
			this.Parameters[name] = value;
		}

		public virtual int ExecuteWithoutExceptions(string commandText)
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
		protected virtual int ExecuteNonQuery(string commandText)
		{
			try
			{
				using (var command = this.BuildCommand(commandText))
					return command.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				if (this.dialect.IsDuplicate(e))
					throw new DuplicateCommitException(e.Message, e);

				throw;
			}
		}

		public virtual IEnumerable<T> ExecuteWithQuery<T>(string queryText, Func<IDataRecord, T> select)
		{
			return this.ExecutePagedQuery(queryText, 0, select);
		}
		public virtual IEnumerable<T> ExecutePagedQuery<T>(
			string queryText, int pageSize, Func<IDataRecord, T> select)
		{
			pageSize = this.dialect.CanPage ? pageSize : 0;
			this.Parameters.Add(this.dialect.Limit, pageSize);
			this.Parameters.Add(this.dialect.Skip, 0);

			var command = this.BuildCommand(queryText);
			IDataParameter skip = null;
			if (this.dialect.CanPage && command.Parameters.Contains(this.dialect.Skip))
				skip = command.Parameters[this.dialect.Skip] as IDataParameter;

			try
			{
				var rows = new PagedEnumeration(command, skip, pageSize).Select(select);
				return new DisposableEnumeration<T>(rows, command, this);
			}
			catch (Exception)
			{
				command.Dispose();
				throw;
			}
		}
		protected virtual IDbCommand BuildCommand(string statement)
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
			param.DbType = type ?? (value == null ? DbType.Binary : param.DbType);
		}
	}
}