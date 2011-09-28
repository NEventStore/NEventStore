namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public class ConnectionScope : ThreadScope<IDbConnection>, IDbConnection
	{
		public ConnectionScope(Func<IDbConnection> factory)
			: base(string.Empty, factory)
		{
		}
		public IDbTransaction BeginTransaction()
		{
			return this.Current.BeginTransaction();
		}
		public IDbTransaction BeginTransaction(IsolationLevel il)
		{
			return this.Current.BeginTransaction(il);
		}
		public void Close()
		{
		}
		public void ChangeDatabase(string databaseName)
		{
			this.Current.ChangeDatabase(databaseName);
		}
		public IDbCommand CreateCommand()
		{
			return this.Current.CreateCommand();
		}
		public void Open()
		{
			this.Current.Open();
		}
		public string ConnectionString
		{
			get { return this.Current.ConnectionString; }
			set { this.Current.ConnectionString = value; }
		}
		public int ConnectionTimeout
		{
			get { return this.Current.ConnectionTimeout; }
		}
		public string Database
		{
			get { return this.Current.Database; }
		}
		public ConnectionState State
		{
			get { return this.Current.State; }
		}
	}
}