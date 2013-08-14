namespace NEventStore.Persistence.MongoPersistence.Tests
{
    using System;
    using NEventStore.Persistence.MongoPersistence;
    using NEventStore.Serialization;

    public class AcceptanceTestMongoPersistenceFactory : MongoPersistenceFactory
    {
        private const string EnvVarKey = "NEventStore.MongoDB";

        public AcceptanceTestMongoPersistenceFactory() : base("", new DocumentObjectSerializer())
        {}

        protected override string GetConnectionString()
        {
            string connectionString = Environment.GetEnvironmentVariable(EnvVarKey, EnvironmentVariableTarget.Process);

            if (connectionString == null)
            {
                string message =
                    string.Format(
                                  "Cannot initialize acceptance tests for Mongo. Cannot find the '{0}' environment variable. Please ensure " +
                                      "you have correctly setup the connection string environment variables. Refer to the " +
                                      "NEventStore wiki for details.",
                        EnvVarKey);
                throw new InvalidOperationException(message);
            }

            connectionString = connectionString.TrimStart('"').TrimEnd('"');

            return connectionString;
        }
    }
}