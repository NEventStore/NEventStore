namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Data;

    public interface IConnectionFactory
    {
        IDbConnection Open();

        Type GetDbProviderFactoryType();
    }
}