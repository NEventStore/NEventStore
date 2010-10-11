namespace EventStore.SqlStorage.DynamicSql.DialectAdapters
{
	using System.Data.Common;

	public abstract class CommonSqlDialectAdapter : IAdaptSqlDialect
	{
		private const string ConstraintViolation = "constraint";
		private const string ParameterNamePrefix = "@";

		public virtual string NormalizeParameterName(string parameterName)
		{
			return ParameterNamePrefix + parameterName;
		}

		public virtual bool IsConstraintViolation(DbException exception)
		{
			return exception.Message.ToLowerInvariant().Contains(ConstraintViolation);
		}
		public abstract bool IsDuplicateKey(DbException exception);
	}
}