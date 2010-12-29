namespace EventStore.Persistence.AcceptanceTests.MongoPersistence
{
    using System.Configuration;
    using Norm;
    using Persistence.MongoPersistence;
    using Serialization;

    public class MongoPersistenceFactory : IPersistenceFactory
    {
        public void Dispose()
        {
            //no-op
        }

        public IPersistStreams Build()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ToString();

            var mongo = Mongo.Create(connectionString);

            return new MongoPersistenceEngine(mongo,new BinarySerializer());
        }
    }
}