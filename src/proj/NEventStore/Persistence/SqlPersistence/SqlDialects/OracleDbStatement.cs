namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
    using System;
    using System.Data;
    using System.Transactions;

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
            var pi = command.GetType().GetProperty("BindByName");
            if(pi!= null) 
                pi.SetValue(command, true, null);
            return command;
        }
    }
}