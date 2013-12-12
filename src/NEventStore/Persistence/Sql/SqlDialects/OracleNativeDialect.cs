namespace NEventStore.Persistence.Sql.SqlDialects
{
    using System;
    using System.Data;
    using System.Reflection;
    using System.Transactions;
    using NEventStore.Persistence.Sql;

    public class OracleNativeDialect : CommonSqlDialect
    {
        private const int UniqueKeyViolation = -2146232008;
        Action<IConnectionFactory, IDbConnection, IDbStatement, byte[]> _addPayloadParamater;

        public override string AppendSnapshotToCommit
        {
            get { return OracleNativeStatements.AppendSnapshotToCommit; }
        }

        public override string CheckpointNumber
        {
            get { return MakeOracleParameter(base.CheckpointNumber); }
        }

        public override string CommitId
        {
            get { return MakeOracleParameter(base.CommitId); }
        }

        public override string CommitSequence
        {
            get { return MakeOracleParameter(base.CommitSequence); }
        }

        public override string CommitStamp
        {
            get { return MakeOracleParameter(base.CommitStamp); }
        }

        public override string CommitStampEnd
        {
            get { return MakeOracleParameter(base.CommitStampEnd); }
        }

        public override string CommitStampStart
        {
            get { return MakeOracleParameter(CommitStampStart); }
        }

        public override string DuplicateCommit
        {
            get { return OracleNativeStatements.DuplicateCommit; }
        }

        public override string GetSnapshot
        {
            get { return OracleNativeStatements.GetSnapshot; }
        }

        public override string GetCommitsFromStartingRevision
        {
            get { return AddOuterTrailingCommitSequence(LimitedQuery(OracleNativeStatements.GetCommitsFromStartingRevision)); }
        }

        public override string GetCommitsFromInstant
        {
            get { return OraclePaging(OracleNativeStatements.GetCommitsFromInstant); }
        }

        public override string GetCommitsFromCheckpoint
        {
            get { return OraclePaging(OracleNativeStatements.GetCommitsSinceCheckpoint); }
        }

        public override string GetUndispatchedCommits
        {
            get { return OraclePaging(base.GetUndispatchedCommits); }
        }

        public override string GetStreamsRequiringSnapshots
        {
            get { return LimitedQuery(OracleNativeStatements.GetStreamsRequiringSnapshots); }
        }

        public override string InitializeStorage
        {
            get { return OracleNativeStatements.InitializeStorage; }
        }

        public override string Limit
        {
            get { return MakeOracleParameter(base.Limit); }
        }

        public override string MarkCommitAsDispatched
        {
            get { return OracleNativeStatements.MarkCommitAsDispatched; }
        }

        public override string PersistCommit
        {
            get { return OracleNativeStatements.PersistCommit; }
        }

        public override string PurgeStorage
        {
            get { return OracleNativeStatements.PurgeStorage; }
        }

        public override string DeleteStream
        {
            get { return OracleNativeStatements.DeleteStream; }
        }

        public override string Drop
        {
            get { return OracleNativeStatements.DropTables; }
        }

        public override string Skip
        {
            get { return MakeOracleParameter(base.Skip); }
        }

        public override string BucketId
        {
            get { return MakeOracleParameter(base.BucketId); }
        }

        public override string StreamId
        {
            get { return MakeOracleParameter(base.StreamId); }
        }

        public override string StreamIdOriginal
        {
            get { return MakeOracleParameter(base.StreamIdOriginal); }
        }

        public override string Threshold
        {
            get { return MakeOracleParameter(base.Threshold); }
        }

        public override string Payload
        {
            get { return MakeOracleParameter(base.Payload); }
        }

        public override string StreamRevision
        {
            get { return MakeOracleParameter(base.StreamRevision); }
        }

        public override string MaxStreamRevision
        {
            get { return MakeOracleParameter(base.MaxStreamRevision); }
        }

        private string AddOuterTrailingCommitSequence(string query)
        {
            return (query.TrimEnd(new[] {';'}) + "\r\n" + OracleNativeStatements.AddCommitSequence);
        }

        public override IDbStatement BuildStatement(TransactionScope scope, IDbConnection connection, IDbTransaction transaction)
        {
            return new OracleDbStatement(this, scope, connection, transaction);
        }

        public override object CoalesceParameterValue(object value)
        {
            if (value is Guid)
            {
                value = ((Guid) value).ToByteArray();
            }

            return value;
        }

        private static string ExtractOrderBy(ref string query)
        {
            int orderByIndex = query.IndexOf("ORDER BY", StringComparison.Ordinal);
            string result = query.Substring(orderByIndex).Replace(";", String.Empty);
            query = query.Substring(0, orderByIndex);

            return result;
        }

        public override bool IsDuplicate(Exception exception)
        {
            return exception.Message.Contains("ORA-00001");
        }

        public override void AddPayloadParamater(IConnectionFactory connectionFactory, IDbConnection connection, IDbStatement cmd, byte[] payload)
        {
            if (_addPayloadParamater == null)
            {
                string dbProviderAssemblyName = connectionFactory.GetDbProviderFactoryType().Assembly.GetName().Name;
                const string oracleManagedDataAcccessAssemblyName = "Oracle.ManagedDataAccess";
                const string oracleDataAcccessAssemblyName = "Oracle.DataAccess";
                if (dbProviderAssemblyName.Equals(oracleManagedDataAcccessAssemblyName, StringComparison.Ordinal))
                {
                    _addPayloadParamater = CreateOraAddPayloadAction(oracleManagedDataAcccessAssemblyName);
                }
                else if (dbProviderAssemblyName.Equals(oracleDataAcccessAssemblyName, StringComparison.Ordinal))
                {
                    _addPayloadParamater = CreateOraAddPayloadAction(oracleDataAcccessAssemblyName);
                }
                else
                {
                    _addPayloadParamater = (connectionFactory2, connection2, cmd2, payload2) 
                        => base.AddPayloadParamater(connectionFactory2, connection2, cmd2, payload2);
                }
            }
            _addPayloadParamater(connectionFactory, connection, cmd, payload);
        }

        private Action<IConnectionFactory, IDbConnection, IDbStatement, byte[]> CreateOraAddPayloadAction(
            string assemblyName)
        {
            Assembly assembly = Assembly.Load(assemblyName);
            var oracleParamaterType = assembly.GetType(assemblyName + ".Client.OracleParameter", true);
            var oracleParamaterValueProperty = oracleParamaterType.GetProperty("Value");
            var oracleBlobType = assembly.GetType(assemblyName + ".Types.OracleBlob", true);
            var oracleBlobWriteMethod = oracleBlobType.GetMethod("Write", new []{ typeof(Byte[]), typeof(int), typeof(int)});
            Type oracleParamapterType = assembly.GetType(assemblyName + ".Client.OracleDbType", true);
            FieldInfo blobField = oracleParamapterType.GetField("Blob");
            var blobDbType = blobField.GetValue(null);

            return (_, connection2, cmd2, payload2) =>
            {
                object payloadParam = Activator.CreateInstance(oracleParamaterType, new[] { Payload, blobDbType });
                ((OracleDbStatement)cmd2).AddParameter(Payload, payloadParam);
                object oracleConnection = ((ConnectionScope)connection2).Current;
                object oracleBlob = Activator.CreateInstance(oracleBlobType, new[] { oracleConnection });
                oracleBlobWriteMethod.Invoke(oracleBlob, new object[] { payload2, 0, payload2.Length });
                oracleParamaterValueProperty.SetValue(payloadParam, oracleBlob, null);
            };
        }

        private static string LimitedQuery(string query)
        {
            query = RemovePaging(query);
            if (query.EndsWith(";"))
            {
                query = query.TrimEnd(new[] {';'});
            }
            string value = OracleNativeStatements.LimitedQueryFormat.FormatWith(query);
            return value;
        }

        private static string MakeOracleParameter(string parameterName)
        {
            return parameterName.Replace('@', ':');
        }

        private static string OraclePaging(string query)
        {
            query = RemovePaging(query);

            string orderBy = ExtractOrderBy(ref query);

            int fromIndex = query.IndexOf("FROM ", StringComparison.Ordinal);
            string from = query.Substring(fromIndex);

            string select = query.Substring(0, fromIndex);

            string value = OracleNativeStatements.PagedQueryFormat.FormatWith(select, orderBy, from);

            return value;
        }

        private static string RemovePaging(string query)
        {
            return query
                .Replace("\n LIMIT @Limit OFFSET @Skip;", ";")
                .Replace("\n LIMIT @Limit;", ";")
                .Replace("WHERE ROWNUM <= :Limit;", ";")
                .Replace("\r\nWHERE ROWNUM <= (:Skip + 1) AND ROWNUM  > :Skip", ";");
        }
    }
}