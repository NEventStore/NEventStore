namespace NEventStore.Persistence.Sql.SqlDialects
{
    public class PostgreSqlDialect : CommonSqlDialect
    {
        public override string InitializeStorage
        {
            get { return PostgreSqlStatements.InitializeStorage; }
        }

        public override string PersistCommit
        {
            get { return PostgreSqlStatements.PersistCommits; }
        }
    }
}