namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System.Data;

	public delegate void NextPageDelegate<T>(IDbCommand command, T latest);
}