namespace EventStore.SqlStorage
{
	using System.Data.Common;

	public interface IAdaptSqlDialect
	{
		string NormalizeParameterName(string parameterName);

		bool IsConstraintViolation(DbException exception);
		bool IsDuplicateKey(DbException exception);
	}
}