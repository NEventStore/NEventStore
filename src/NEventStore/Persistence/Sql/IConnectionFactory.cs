namespace NEventStore.Persistence.Sql
{
    using System.Data;

    public interface IConnectionFactory
    {
        IDbConnection Open();
    }
}