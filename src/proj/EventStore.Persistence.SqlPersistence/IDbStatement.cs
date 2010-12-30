namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;

	public interface IDbStatement : IDisposable
	{
		void AddParameter(string name, object value);

		int Execute(string commandText);
		int ExecuteWithSuppression(string commandText);

		IEnumerable<T> ExecuteWithQuery<T>(string queryText, Func<IDataRecord, T> select);
	}
}