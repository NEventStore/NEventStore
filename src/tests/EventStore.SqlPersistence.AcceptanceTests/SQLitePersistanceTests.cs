using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventStore.Persistence.AcceptanceTests;
using Machine.Specifications;

namespace EventStore.SqlPersistence.AcceptanceTests
{
    public class when_a_commit_attempt_is_successfully_committed_using_SQLite_persistence : when_a_commit_attempt_is_successfully_committed
    {
        Establish context = () =>
            persistence = TestSqlPersistenceFactory.CreateSqlPersistence("SQLite");

        Because of = () => because();

        Behaves_like<ASuccessfulCommit> a_successful_commit;
    }
}
