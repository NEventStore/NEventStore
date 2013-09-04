namespace NEventStore.Persistence.Sql.SqlDialects
{
    using System;
    using System.Data;
    using System.Reflection;
    using System.Transactions;
    using NEventStore.Persistence.Sql;

    public class OracleDbStatement : CommonDbStatement
    {
        public OracleDbStatement(ISqlDialect dialect, TransactionScope scope, IDbConnection connection, IDbTransaction transaction)
            : base(dialect, scope, connection, transaction)
        {}

        public override void AddParameter(string name, object value)
        {
            name = name.Replace('@', ':');

            if (value is Guid)
            {
                base.AddParameter(name, ((Guid) value).ToByteArray());
            }
            else
            {
                base.AddParameter(name, value);
            }
        }

        public override int ExecuteNonQuery(string commandText)
        {
            try
            {
                using (IDbCommand command = BuildCommand(commandText))
                    return command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                if (Dialect.IsDuplicate(e))
                {
                    throw new UniqueKeyViolationException(e.Message, e);
                }

                throw;
            }
        }

        protected override IDbCommand BuildCommand(string statement)
        {
            IDbCommand command = base.BuildCommand(statement);
            PropertyInfo pi = command.GetType().GetProperty("BindByName");
            if (pi != null)
            {
                pi.SetValue(command, true, null);
            }
            return command;
        }
    }
}