namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Data;
    using System.Transactions;
    using FakeItEasy;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using NEventStore.Serialization;
    using Xunit;
    using Xunit.Should;

    public class when_persisting_a_commit : SpecificationBase
    {
        private InheritedSqlPersistenceEngine _sqlPersistenceEngine;

        protected override void Context()
        {
            var fakeConnectionFactory = A.Fake<IConnectionFactory>();
            var fakeSqlDialect = A.Fake<ISqlDialect>();
            var fakeDbStatement = A.Fake<IDbStatement>();
            A.CallTo(() => fakeSqlDialect.BuildStatement(
                A<TransactionScope>.Ignored,
                A<IDbConnection>.Ignored,
                A<IDbTransaction>.Ignored))
                .Returns(fakeDbStatement);
            A.CallTo(() => fakeDbStatement.ExecuteScalar(A<string>.Ignored)).Returns(1);
            var fakeSerialize = A.Fake<ISerialize>();
            _sqlPersistenceEngine = new InheritedSqlPersistenceEngine(fakeConnectionFactory, fakeSqlDialect, fakeSerialize,TransactionScopeOption.Suppress, 128);
        }

        protected override void Because()
        {
            _sqlPersistenceEngine.Commit(
                new CommitAttempt("streamid", 1, Guid.NewGuid(), 1, SystemTime.UtcNow, null, new[] {new EventMessage()}));
        }

        [Fact]
        public void should_raise_BeforePersistCommit_event()
        {
            _sqlPersistenceEngine.RaisedCommand.ShouldNotBeNull();
            _sqlPersistenceEngine.RaisedCommitAttempt.ShouldNotBeNull();
        }

        private class InheritedSqlPersistenceEngine : SqlPersistenceEngine
        {
            private IDbStatement _raisedCommand;
            private CommitAttempt _raisedCommitAttempt;

            public InheritedSqlPersistenceEngine(
                IConnectionFactory connectionFactory,
                ISqlDialect dialect,
                ISerialize serializer,
                TransactionScopeOption scopeOption, int pageSize) 
                : base(connectionFactory, dialect, serializer, scopeOption, pageSize)
            {}

            public IDbStatement RaisedCommand
            {
                get { return _raisedCommand; }
            }

            public CommitAttempt RaisedCommitAttempt
            {
                get { return _raisedCommitAttempt; }
            }

            protected override void OnPersistCommit(IDbStatement cmd, CommitAttempt attempt)
            {
                _raisedCommand = cmd;
                _raisedCommitAttempt = attempt;
            }
        }
    }
}