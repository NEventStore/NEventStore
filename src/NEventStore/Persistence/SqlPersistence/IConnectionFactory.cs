namespace NEventStore.Persistence.SqlPersistence
{
    using System.Data;

    public interface IConnectionFactory
    {
        IDbConnection OpenMaster(string streamId);

        IDbConnection OpenReplica(string streamId);
    }
}