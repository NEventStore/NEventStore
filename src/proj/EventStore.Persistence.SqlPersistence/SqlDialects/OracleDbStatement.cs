using System;
using System.Transactions;
using System.Data;


namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
    public class OracleDbStatement : CommonDbStatement
    {
        public OracleDbStatement(ISqlDialect dialect, TransactionScope scope, IDbConnection connection, IDbTransaction transaction)
            : base(dialect, scope, connection, transaction) { }

        public override void AddParameter(string name, object value)
        {
            name = name.Replace('@', ':');

            if (value is Guid)
                base.AddParameter(name, ((Guid)value).ToByteArray());
            else
                base.AddParameter(name, value);
        }
        public override int ExecuteNonQuery(string commandText)
        {
            try
            {
                using (var command = this.BuildCommand(commandText))
                    return command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                if (this.Dialect.IsDuplicate(e))
                    throw new UniqueKeyViolationException(e.Message, e);

                throw;
            }
        }
        protected override IDbCommand BuildCommand(string statement)
        {
            var command = base.BuildCommand(statement);
            command.GetType().GetProperty("BindByName").SetValue(command, true, null);
            return command;
        }
    }
}