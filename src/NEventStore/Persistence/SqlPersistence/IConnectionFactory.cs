namespace NEventStore.Persistence.SqlPersistence
{
    using System.Data;

    public interface IConnectionFactory
    {
        IDbConnection Open();
    }
}