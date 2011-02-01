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

		public virtual IDbConnection OpenForReading(Guid streamId)
		{
			return this.openConnection(streamId);
		}
		public virtual IDbConnection OpenForWriting(Guid streamId)
		{
			return this.openConnection(streamId);
		}
	}
}