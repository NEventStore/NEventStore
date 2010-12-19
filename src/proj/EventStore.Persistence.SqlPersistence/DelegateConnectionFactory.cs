namespace EventStore.Persistence.SqlPersistence
{
	using System;
	using System.Data;

	public class DelegateConnectionFactory : IConnectionFactory
	{
		private readonly Func<Guid, IDbConnection> openConnection;

		public DelegateConnectionFactory(Func<Guid, IDbConnection> openConnection)
		{
			this.openConnection = openConnection;
		}

		public virtual IDbConnection Open(Guid streamId)
		{
			return this.openConnection(streamId);
		}
	}
}