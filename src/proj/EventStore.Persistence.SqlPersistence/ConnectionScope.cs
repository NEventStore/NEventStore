namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public class ConnectionScope : ThreadScope<IDbConnection>, IDbConnection
	{
		public ConnectionScope(string connectionName, Func<IDbConnection> factory)
			: base(connectionName, factory)
		{
		}
		IDbTransaction IDbConnection.BeginTransaction()
		{
			return this.Current.BeginTransaction();
		}
		IDbTransaction IDbConnection.BeginTransaction(IsolationLevel il)
		{
			return this.Current.BeginTransaction(il);
		}
		void IDbConnection.Close()
		{
			// no-op--let Dispose do the real work.
		}
		void IDbConnection.ChangeDatabase(string databaseName)
		{
			this.Current.ChangeDatabase(databaseName);
		}
		IDbCommand IDbConnection.CreateCommand()
		{
			return this.Current.CreateCommand();
		}
		void IDbConnection.Open()
		{
			this.Current.Open();
		}
		string IDbConnection.ConnectionString
		{
			get { return this.Current.ConnectionString; }
			set { this.Current.ConnectionString = value; }
		}
		int IDbConnection.ConnectionTimeout
		{
			get { return this.Current.ConnectionTimeout; }
		}
		string IDbConnection.Database
		{
			get { return this.Current.Database; }
		}
		ConnectionState IDbConnection.State
		{
			get { return this.Current.State; }
		}
	}
}