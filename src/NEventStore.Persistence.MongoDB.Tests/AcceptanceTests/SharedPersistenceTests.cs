using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEventStore.Persistence.MongoDB.Tests.AcceptanceTests
{
    using System.Diagnostics;
    using NEventStore.Diagnostics;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class when_a_commit_is_persisted_from_a_second_process : SpecificationBase
    {
        IPersistStreams _process1;
        ICommit _commit1;
        IPersistStreams _process2;
        ICommit _commit2;

        protected override void Context()
        {
            _process1 = new AcceptanceTestMongoPersistenceFactory().Build();
            _process1.Initialize();
            _commit1 = _process1.Commit(Guid.NewGuid().ToString().BuildAttempt());

            _process2 = new AcceptanceTestMongoPersistenceFactory().Build();
            _process2.Initialize();
        }

        protected override void Because()
        {
            _commit2 = _process2.Commit(Guid.NewGuid().ToString().BuildAttempt());
        }

        [Fact]
        public void should_have_a_checkpoint_greater_than_the_previous_commit_on_the_other_process()
        {
            var chkNum1 = LongCheckpoint.Parse(_commit1.CheckpointToken);
            var chkNum2 = LongCheckpoint.Parse(_commit2.CheckpointToken);
                
            chkNum2.ShouldBeGreaterThan(chkNum1);
        }

        protected override void Cleanup()
        {
            _process1.Drop();
            _process1.Dispose();
        }
    }
}
