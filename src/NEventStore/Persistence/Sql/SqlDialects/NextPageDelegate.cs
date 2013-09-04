namespace NEventStore.Persistence.Sql.SqlDialects
{
    using System.Data;

    public delegate void NextPageDelegate(IDbCommand command, IDataRecord current);
}