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

		IEnumerable<T> ExecuteWithQuery<T>(string queryText, Func<IDataRecord, T> select);
		IEnumerable<T> ExecutePagedQuery<T>(
			string queryText, Func<IDataRecord, T> select, NextPageDelegate<T> onNextPage, int pageSize);
	}
}