namespace NEventStore.Persistence.SqlPersistence.SqlDialects
{
    public class PostgreSqlDialect : CommonSqlDialect
    {
        public override string InitializeStorage
        {
            get { return PostgreSqlStatements.InitializeStorage; }
        }

        public override string MarkCommitAsDispatched
        {
            get { return base.MarkCommitAsDispatched.Replace("1", "true"); }
        }

        public override string PersistCommit
        {
            get { throw new System.NotImplementedException(); }
        }

        public override string GetUndispatchedCommits
        {
            get { return base.GetUndispatchedCommits.Replace("0", "false"); }
        }
    }
}