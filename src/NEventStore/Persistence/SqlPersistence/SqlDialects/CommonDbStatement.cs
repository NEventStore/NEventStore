namespace NEventStore.Persistence.SqlPersistence.SqlDialects
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Transactions;
    using NEventStore.Logging;

    public class CommonDbStatement : IDbStatement
    {
        private const int InfinitePageSize = 0;
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (CommonDbStatement));
        private readonly IDbConnection _connection;
        private readonly ISqlDialect _dialect;
        private readonly TransactionScope _scope;
        private readonly IDbTransaction _transaction;

        public CommonDbStatement(
            ISqlDialect dialect,
            TransactionScope scope,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            Parameters = new Dictionary<string, object>();

            _dialect = dialect;
            _scope = scope;
            _connection = connection;
            _transaction = transaction;
        }

        protected IDictionary<string, object> Parameters { get; private set; }

        protected ISqlDialect Dialect
        {
            get { return _dialect; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual int PageSize { get; set; }

        public virtual void AddParameter(string name, object value)
        {
            Logger.Debug(Messages.AddingParameter, name);
            Parameters[name] = _dialect.CoalesceParameterValue(value);
        }

        public virtual int ExecuteWithoutExceptions(string commandText)
        {
            try
            {
                return ExecuteNonQuery(commandText);
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
                using (IDbCommand command = BuildCommand(commandText))
                    return command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                if (_dialect.IsDuplicate(e))
                {
                    throw new UniqueKeyViolationException(e.Message, e);
                }

                throw;
            }
        }

        public virtual object ExecuteScalar(string commandText)
        {
            using (IDbCommand command = BuildCommand(commandText))
                return command.ExecuteScalar();
        }

        public virtual IEnumerable<IDataRecord> ExecuteWithQuery(string queryText)
        {
            return ExecuteQuery(queryText, (query, latest) => { }, InfinitePageSize);
        }

        public virtual IEnumerable<IDataRecord> ExecutePagedQuery(string queryText, NextPageDelegate nextpage)
        {
            int pageSize = _dialect.CanPage ? PageSize : InfinitePageSize;
            if (pageSize > 0)
            {
                Logger.Verbose(Messages.MaxPageSize, pageSize);
                Parameters.Add(_dialect.Limit, pageSize);
            }

            return ExecuteQuery(queryText, nextpage, pageSize);
        }

        protected virtual void Dispose(bool disposing)
        {
            Logger.Verbose(Messages.DisposingStatement);

            if (_transaction != null)
            {
                _transaction.Dispose();
            }

            if (_connection != null)
            {
                _connection.Dispose();
            }

            if (_scope != null)
            {
                _scope.Dispose();
            }
        }

        protected virtual IEnumerable<IDataRecord> ExecuteQuery(string queryText, NextPageDelegate nextpage, int pageSize)
        {
            Parameters.Add(_dialect.Skip, 0);
            IDbCommand command = BuildCommand(queryText);

            try
            {
                return new PagedEnumerationCollection(_scope, _dialect, command, nextpage, pageSize, this);
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
            IDbCommand command = _connection.CreateCommand();
            command.Transaction = _transaction;
            command.CommandText = statement;

            Logger.Verbose(Messages.ClientControlledTransaction, _transaction != null);
            Logger.Verbose(Messages.CommandTextToExecute, statement);

            BuildParameters(command);

            return command;
        }

        protected virtual void BuildParameters(IDbCommand command)
        {
            foreach (var item in Parameters)
            {
                BuildParameter(command, item.Key, item.Value);
            }
        }

        protected virtual void BuildParameter(IDbCommand command, string name, object value)
        {
            IDbDataParameter parameter = command.CreateParameter();
            parameter.ParameterName = name;
            SetParameterValue(parameter, value, null);

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