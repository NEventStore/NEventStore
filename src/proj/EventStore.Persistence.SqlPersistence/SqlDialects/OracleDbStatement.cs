using System;
using System.Data;
using System.Reflection;
using System.Transactions;
//using Oracle.DataAccess.Client;

namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
    public class OracleDbStatement : CommonDbStatement
    {
        private readonly PropertyInfo oracleCommandBindByName;
        private const string assemblyNameTemplate = "Oracle.DataAccess,Version={0},Culture=neutral,PublicKeyToken=89b483f429c47342";
        private const string commandTypeName = "Oracle.DataAccess.Client.OracleCommand";

        private const string version = "2.102.2.20";
        public Func<string> getVersion = () => version;

        public OracleDbStatement(ISqlDialect dialect, TransactionScope scope, IDbConnection connection, IDbTransaction transaction)
			: base(dialect, scope, connection, transaction)
        {
            var fullyQualifiedAssemblyName = string.Format(assemblyNameTemplate, getVersion());
            var name = string.Format("{0}, {1}", commandTypeName, fullyQualifiedAssemblyName);
            var commandType = Type.GetType(name);
            oracleCommandBindByName = commandType.GetProperty("BindByName");
        }

        public override void AddParameter(string name, object value)
        {
            name = name.Replace("@", ":");

            if (value is Guid)
                base.AddParameter(name, ((Guid)value).ToByteArray());
            else
                base.AddParameter(name, value);
        }

        protected override IDbCommand BuildCommand(string statement)
        {
            var command = base.BuildCommand(statement);
            command.CommandText = command.CommandText.Replace("\r\n", "\n");
            oracleCommandBindByName.SetValue(command, true, null);
            return command;
        }

        public override int ExecuteNonQuery(string commandText)
        {
            try
            {
                using (var command = this.BuildCommand(commandText))
                {
                    oracleCommandBindByName.SetValue(command, true, null);
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                if (this.Dialect.IsDuplicate(e))
                    throw new UniqueKeyViolationException(e.Message, e);

                throw;
            }
        }
    }
}
