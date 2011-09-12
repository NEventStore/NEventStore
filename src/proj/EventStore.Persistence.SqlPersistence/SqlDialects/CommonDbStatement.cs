namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using Logging;

	public class CommonDbStatement : IDbStatement
	{
		private const int InfinitePageSize = 0;
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(CommonDbStatement));
		private readonly ISqlDialect dialect;
		private readonly IDbTransaction transaction;
		private readonly IDbConnection connection;
		private readonly IDisposable[] resources;

		protected IDictionary<string, object> Parameters { get; private set; }

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
			Logger.Verbose(Messages.DisposingStatement);

			if (this.transaction != null)
				this.transaction.Dispose();

			if (this.connection != null)
				this.connection.Dispose();

			foreach (var resource in this.resources.Reverse().Where(resource => resource != null))
				resource.Dispose(); // dispose from the inside out
		}

		public virtual void AddParameter(string name, object value)
		{
			Logger.Debug(Messages.AddingParameter, name);
			this.Parameters[name] = this.dialect.CoalesceParameterValue(value);
		}

		public virtual int ExecuteWithoutExceptions(string commandText)
		{
			try
			{
				return this.Execute(commandText);
			}
			catch (Exception)
			{
				Logger.Debug(Messages.ExceptionSuppressed);
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
				Logger.Debug(Messages.CommandThrewException, e.GetType());
				if (!this.dialect.IsDuplicate(e))
					throw;

				Logger.Debug(Messages.DuplicateCommit);
				throw new DuplicateCommitException(e.Message, e);
			}
		}

		public virtual IEnumerable<T> ExecuteWithQuery<T>(string queryText, Func<IDataRecord, T> select)
		{
			return this.ExecutePagedQuery(queryText, select, (query, latest) => { }, InfinitePageSize);
		}
		public virtual IEnumerable<T> ExecutePagedQuery<T>(
			string queryText, Func<IDataRecord, T> select, NextPageDelegate<T> onNextPage, int pageSize)
		{
			pageSize = this.dialect.CanPage ? pageSize : InfinitePageSize;
			if (pageSize > 0)
			{
				Logger.Verbose(Messages.MaxPageSize, pageSize);
				this.Parameters.Add(this.dialect.Limit, pageSize);
			}

			var command = this.BuildCommand(queryText);

			try
			{
				var rows = new PagedEnumeration<T>(command, select, onNextPage, pageSize);
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
			Logger.Verbose(Messages.CreatingCommand);
			var command = this.connection.CreateCommand();
			command.Transaction = this.transaction;
			command.CommandText = statement;

			Logger.Verbose(Messages.ClientControlledTransaction, this.transaction != null);
			Logger.Verbose(Messages.CommandTextToExecute, statement);

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

			Logger.Verbose(Messages.BindingParameter, name, parameter.Value);
			command.Parameters.Add(parameter);
		}
		protected virtual void SetParameterValue(IDataParameter param, object value, DbType? type)
		{
			param.Value = value ?? DBNull.Value;
			param.DbType = type ?? (value == null ? DbType.Binary : param.DbType);
		}
	}
}