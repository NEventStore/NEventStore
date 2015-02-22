namespace NEventStore.Persistence.Sql
{
    using System;
    using System.Data;
    using System.Transactions;
    using FakeItEasy;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using NEventStore.Persistence.Sql.SqlDialects;
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

    public class when_hasher_returns_null : SpecificationBase
    {
        private SqlPersistenceEngine _sqlPersistenceEngine;
        private Exception _exception;

        protected override void Context()
        {
            _sqlPersistenceEngine = new SqlPersistenceEngine(
                A.Fake<IConnectionFactory>(),
                A.Fake<ISqlDialect>(),
                A.Fake<ISerialize>(),
                TransactionScopeOption.Suppress,
                128,
                new DelegateStreamIdHasher(streamId => null));
        }

        protected override void Because()
        {
            _exception = Catch.Exception(() => _sqlPersistenceEngine.Commit(
                new CommitAttempt("streamId", 1, Guid.NewGuid(), 1, SystemTime.UtcNow, null, new[] {new EventMessage()})));
        }

        [Fact]
        public void should_raise_invalid_operation_exception()
        {
            _exception.ShouldBeInstanceOf<InvalidOperationException>();
        }
    }

    public class when_hasher_returns_whitespace: SpecificationBase
    {
        private SqlPersistenceEngine _sqlPersistenceEngine;
        private Exception _exception;

        protected override void Context()
        {
            _sqlPersistenceEngine = new SqlPersistenceEngine(
                A.Fake<IConnectionFactory>(),
                A.Fake<ISqlDialect>(),
                A.Fake<ISerialize>(),
                TransactionScopeOption.Suppress,
                128,
                new DelegateStreamIdHasher(streamId => " "));
        }

        protected override void Because()
        {
            _exception = Catch.Exception(() => _sqlPersistenceEngine.Commit(
                new CommitAttempt("streamId", 1, Guid.NewGuid(), 1, SystemTime.UtcNow, null, new[] { new EventMessage() })));
        }

        [Fact]
        public void should_raise_invalid_operation_exception()
        {
            _exception.ShouldBeInstanceOf<InvalidOperationException>();
        }
    }

    public class when_hasher_returns_empty : SpecificationBase
    {
        private SqlPersistenceEngine _sqlPersistenceEngine;
        private Exception _exception;

        protected override void Context()
        {
            _sqlPersistenceEngine = new SqlPersistenceEngine(
                A.Fake<IConnectionFactory>(),
                A.Fake<ISqlDialect>(),
                A.Fake<ISerialize>(),
                TransactionScopeOption.Suppress,
                128,
                new DelegateStreamIdHasher(streamId => string.Empty));
        }

        protected override void Because()
        {
            _exception = Catch.Exception(() => _sqlPersistenceEngine.Commit(
                new CommitAttempt("streamId", 1, Guid.NewGuid(), 1, SystemTime.UtcNow, null, new[] { new EventMessage() })));
        }

        [Fact]
        public void should_raise_invalid_operation_exception()
        {
            _exception.ShouldBeInstanceOf<InvalidOperationException>();
        }
    }

    public class when_hasher_returns_string_longer_than_40_characters : SpecificationBase
    {
        private SqlPersistenceEngine _sqlPersistenceEngine;
        private Exception _exception;

        protected override void Context()
        {
            _sqlPersistenceEngine = new SqlPersistenceEngine(
                A.Fake<IConnectionFactory>(),
                A.Fake<ISqlDialect>(),
                A.Fake<ISerialize>(),
                TransactionScopeOption.Suppress,
                128,
                new DelegateStreamIdHasher(streamId => "0123456789012345678901234567890123456789X"));
        }

        protected override void Because()
        {
            _exception = Catch.Exception(() => _sqlPersistenceEngine.Commit(
                new CommitAttempt("streamId", 1, Guid.NewGuid(), 1, SystemTime.UtcNow, null, new[] { new EventMessage() })));
        }

        [Fact]
        public void should_raise_invalid_operation_exception()
        {
            _exception.ShouldBeInstanceOf<InvalidOperationException>();
        }
    }

    public class when_getting_checkpoint_with_null_token : SpecificationBase
    {
        private ICheckpoint _checkpoint;

        protected override void Because()
        {
            var persistence = new SqlPersistenceFactory("Connection",
                new BinarySerializer(),
                new MsSqlDialect()).Build();

            _checkpoint = persistence.GetCheckpoint();
        }

        [Fact]
        public void should_not_be_null()
        {
            _checkpoint.ShouldNotBeNull();
        }
    }
}
