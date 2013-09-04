namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using NEventStore.Persistence.Sql.SqlDialects;

    public interface IDbStatement : IDisposable
    {
        int PageSize { get; set; }

        void AddParameter(string name, object value);

        int ExecuteNonQuery(string commandText);

        int ExecuteWithoutExceptions(string commandText);

        object ExecuteScalar(string commandText);

        IEnumerable<IDataRecord> ExecuteWithQuery(string queryText);

        IEnumerable<IDataRecord> ExecutePagedQuery(string queryText, NextPageDelegate nextpage);
    }
}