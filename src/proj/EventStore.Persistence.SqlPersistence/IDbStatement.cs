namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;

	public interface IDbStatement : IDisposable
	{
		void AddParameter(string name, object value);

		int Execute(string commandText);
		int ExecuteWithoutExceptions(string commandText);

		IEnumerable<T> ExecuteWithQuery<T>(string queryText, Func<IDataRecord, T> select);
		IEnumerable<T> ExecutePagedQuery<T>(string queryText, int pageSize, Func<IDataRecord, T> select);
	}
}