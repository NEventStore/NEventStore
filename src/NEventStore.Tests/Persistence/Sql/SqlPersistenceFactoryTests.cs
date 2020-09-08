namespace NEventStore.Persistence.Sql
{
    using System;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using NEventStore.Persistence.Sql.SqlDialects;
    using NEventStore.Serialization;
    using Xunit;
    using Xunit.Should;

    public class when_creating_sql_persistence_factory_with_oracle_native_dialect : SpecificationBase
    {
        private Exception _exception;

        protected override void Because()
        {
            _exception = Catch.Exception(() => new SqlPersistenceFactory("Connection",
                new BinarySerializer(),
                null,
                new OracleNativeDialect()).Build());
        }

        [Fact]
        public void should_not_throw()
        {
           _exception.ShouldBeNull();
        }
    }
}