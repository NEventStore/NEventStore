namespace NEventStore.Persistence.SqlPersistence.SqlDialects
{
    using System;
    using System.Reflection;

    public class MySqlDialect : CommonSqlDialect
    {
        private const int UniqueKeyViolation = 1062;

        public override string InitializeStorage
        {
            get { return MySqlStatements.InitializeStorage; }
        }

        public override string PersistCommit
        {
            get { throw new NotImplementedException(); }
        }

        public override string AppendSnapshotToCommit
        {
            get { return base.AppendSnapshotToCommit.Replace("/*FROM DUAL*/", "FROM DUAL"); }
        }

        public override string MarkCommitAsDispatched
        {
            get { return base.MarkCommitAsDispatched.Replace("1", "true"); }
        }

        public override string GetUndispatchedCommits
        {
            get { return base.GetUndispatchedCommits.Replace("0", "false"); }
        }

        public override object CoalesceParameterValue(object value)
        {
            if (value is Guid)
            {
                return ((Guid) value).ToByteArray();
            }

            if (value is DateTime)
            {
                return ((DateTime) value).Ticks;
            }

            return value;
        }

        public override bool IsDuplicate(Exception exception)
        {
            PropertyInfo property = exception.GetType().GetProperty("Number");
            return UniqueKeyViolation == (int) property.GetValue(exception, null);
        }
    }
}