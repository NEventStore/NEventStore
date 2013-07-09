namespace EventStore.Persistence.SqlPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using SqlDialects;

    public interface IDbStatement : IDisposable
	{
		void AddParameter(string name, object value);

		int ExecuteNonQuery(string commandText);
		int ExecuteWithoutExceptions(string commandText);

		object ExecuteScalar(string commandText);

		int PageSize { get; set; }

		IEnumerable<IDataRecord> ExecuteWithQuery(string queryText);
		IEnumerable<IDataRecord> ExecutePagedQuery(string queryText, NextPageDelegate nextpage);
	}
}