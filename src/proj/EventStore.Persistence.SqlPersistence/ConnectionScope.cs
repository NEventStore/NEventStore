namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public class ConnectionScope : ThreadScope<IDbConnection>
	{
		public ConnectionScope(Func<IDbConnection> factory)
			: base(factory)
		{
		}
	}
}