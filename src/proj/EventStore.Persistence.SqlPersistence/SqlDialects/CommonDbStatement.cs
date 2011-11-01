namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Transactions;
	using Logging;

	public class CommonDbStatement : IDbStatement
	{
		private const int InfinitePageSize = 0;
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(CommonDbStatement));
		private readonly ISqlDialect dialect;
		private readonly TransactionScope scope;
		private readonly IDbConnection connection;
		private readonly IDbTransaction transaction;

		protected IDictionary<string, object> Parameters { get; private set; }

		public CommonDbStatement(
			ISqlDialect dialect,
			TransactionScope scope,
			IDbConnection connection,
			IDbTransaction transaction)
		{
			this.Parameters = new Dictionary<string, object>();

			this.dialect = dialect;
			this.scope = scope;
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
			Logger.Verbose(Messages.DisposingStatement);

			if (this.transaction != null)
				this.transaction.Dispose();

			if (this.connection != null)
				this.connection.Dispose();

			if (this.scope != null)
				this.scope.Dispose();
		}

		public virtual int PageSize { get; set; }

		public virtual void AddParameter(string name, object value)
		{
			Logger.Debug(Messages.AddingParameter, name);
			this.Parameters[name] = this.dialect.CoalesceParameterValue(value);
		}

		public virtual int ExecuteWithoutExceptions(string commandText)
		{
			try
			{
				return this.ExecuteNonQuery(commandText);
			}
			catch (Exception)
			{
				Logger.Debug(Messages.ExceptionSuppressed);
				return 0;
			}
		}
		public virtual int ExecuteNonQuery(string commandText)
		{
			try
			{
				using (var command = this.BuildCommand(commandText))
					return command.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				if (this.dialect.IsDuplicate(e))
					throw new UniqueKeyViolationException(e.Message, e);

				throw;
			}
		}

		public virtual object ExecuteScalar(string commandText)
		{
			using (var command = this.BuildCommand(commandText))
				return command.ExecuteScalar();
		}

		public virtual IEnumerable<IDataRecord> ExecuteWithQuery(string queryText)
		{
			return this.ExecuteQuery(queryText, (query, latest) => { }, InfinitePageSize);
		}
		public virtual IEnumerable<IDataRecord> ExecutePagedQuery(string queryText, NextPageDelegate nextpage)
		{
			var pageSize = this.dialect.CanPage ? this.PageSize : InfinitePageSize;
			if (pageSize > 0)
			{
				Logger.Verbose(Messages.MaxPageSize, pageSize);
				this.Parameters.Add(this.dialect.Limit, pageSize);
			}

			return this.ExecuteQuery(queryText, nextpage, pageSize);
		}
		protected virtual IEnumerable<IDataRecord> ExecuteQuery(string queryText, NextPageDelegate nextpage, int pageSize)
		{
			var command = this.BuildCommand(queryText);

			try
			{
				return new PagedEnumerationCollection(command, nextpage, pageSize, this.scope, this);
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