namespace NEventStore.DispatcherTests
{
    using System.Linq;
    using Moq;
    using NEventStore.Dispatcher;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;

    public class when_instantiating_the_synchronous_dispatch_scheduler : SpecificationBase
    {
        private readonly Mock<IDispatchCommits> _dispatcher = new Mock<IDispatchCommits>();
        private readonly Mock<IPersistStreams> _persistence = new Mock<IPersistStreams>();
        private ICommit[] _commits;

        protected override void Context()
        {
            _commits = new[]
            {
                CommitHelper.Create(),
                CommitHelper.Create()
            };

            _persistence.Setup(x => x.Initialize());
            _persistence.Setup(x => x.GetUndispatchedCommits()).Returns(_commits);
            _dispatcher.Setup(x => x.Dispatch(_commits.First()));
            _dispatcher.Setup(x => x.Dispatch(_commits.Last()));
        }

        protected override void Because()
        {
            new SynchronousDispatchScheduler(_dispatcher.Object, _persistence.Object);
        }

        [Fact]
        public void should_initialize_the_persistence_engine()
        {
            _persistence.Verify(x => x.Initialize(), Times.Once());
        }

        [Fact]
        public void should_get_the_set_of_undispatched_commits()
        {
            _persistence.Verify(x => x.GetUndispatchedCommits(), Times.Once());
        }

        [Fact]
        public void should_provide_the_commits_to_the_dispatcher()
        {
            _dispatcher.VerifyAll();
        }
    }

    public class when_synchronously_scheduling_a_commit_for_dispatch : SpecificationBase
    {
        private readonly ICommit _commitAttempt = CommitHelper.Create();
        private readonly Mock<IDispatchCommits> _dispatcher = new Mock<IDispatchCommits>();
        private readonly Mock<IPersistStreams> _persistence = new Mock<IPersistStreams>();
        private SynchronousDispatchScheduler _dispatchScheduler;

        protected override void Context()
        {
            _dispatcher.Setup(x => x.Dispatch(_commitAttempt));
            _persistence.Setup(x => x.MarkCommitAsDispatched(_commitAttempt));

            _dispatchScheduler = new SynchronousDispatchScheduler(_dispatcher.Object, _persistence.Object);
        }

        protected override void Because()
        {
            _dispatchScheduler.ScheduleDispatch(_commitAttempt);
        }

        [Fact]
        public void should_provide_the_commit_to_the_dispatcher()
        {
            _dispatcher.Verify(x => x.Dispatch(_commitAttempt), Times.Once());
        }

        [Fact]
        public void should_mark_the_commit_as_dispatched()
        {
            _persistence.Verify(x => x.MarkCommitAsDispatched(_commitAttempt), Times.Once());
        }
    }

    public class when_disposing_the_synchronous_dispatch_scheduler : SpecificationBase
    {
        private readonly Mock<IDispatchCommits> _dispatcher = new Mock<IDispatchCommits>();
        private readonly Mock<IPersistStreams> _persistence = new Mock<IPersistStreams>();
        private SynchronousDispatchScheduler _dispatchScheduler;

        protected override void Context()
        {
            _dispatcher.Setup(x => x.Dispose());
            _persistence.Setup(x => x.Dispose());
            _dispatchScheduler = new SynchronousDispatchScheduler(_dispatcher.Object, _persistence.Object);
        }

        protected override void Because()
        {
            _dispatchScheduler.Dispose();
            _dispatchScheduler.Dispose();
        }

        [Fact]
        public void should_dispose_the_underlying_dispatcher_exactly_once()
        {
            _dispatcher.Verify(x => x.Dispose(), Times.Once());
        }

        [Fact]
        public void should_dispose_the_underlying_persistence_infrastructure_exactly_once()
        {
            _dispatcher.Verify(x => x.Dispose(), Times.Once());
        }
    }
}