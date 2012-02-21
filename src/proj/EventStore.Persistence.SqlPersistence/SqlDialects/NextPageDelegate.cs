namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Data;

	public delegate void NextPageDelegate(IDbCommand command, IDataRecord current);
}