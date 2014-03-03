namespace NEventStore.DispatcherTests
{
    using FakeItEasy;
    using NEventStore.Dispatcher;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;

    public class when_instantiating_the_synchronous_dispatch_scheduler : SpecificationBase
    {
        private readonly IDispatchCommits _dispatcher = A.Fake<IDispatchCommits>();
        private readonly IPersistStreams _persistence = A.Fake<IPersistStreams>();
        private ICommit[] _commits;
        private ICommit _firstCommit, _lastCommit;
        private SynchronousDispatchScheduler _dispatchScheduler;

        protected override void Context()
        {
            _commits = new[]
            {
                _firstCommit = CommitHelper.Create(),
                _lastCommit = CommitHelper.Create()
            };

            A.CallTo(() => _persistence.GetUndispatchedCommits()).Returns(_commits);
        }

        protected override void Because()
        {
            _dispatchScheduler = new SynchronousDispatchScheduler(_dispatcher, _persistence);
            _dispatchScheduler.Start();
        }

        [Fact]
        public void should_initialize_the_persistence_engine()
        {
            A.CallTo(() => _persistence.Initialize()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_get_the_set_of_undispatched_commits()
        {
            A.CallTo(() => _persistence.GetUndispatchedCommits()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_provide_the_commits_to_the_dispatcher()
        {
            A.CallTo(() => _dispatcher.Dispatch(_firstCommit)).MustHaveHappened();
            A.CallTo(() => _dispatcher.Dispatch(_lastCommit)).MustHaveHappened();
        }
    }

    public class when_synchronously_scheduling_a_commit_for_dispatch : SpecificationBase
    {
        private readonly ICommit _commit = CommitHelper.Create();
        private readonly IDispatchCommits _dispatchCommits = A.Fake<IDispatchCommits>();
        private readonly IPersistStreams _persistStreams = A.Fake<IPersistStreams>();
        private SynchronousDispatchScheduler _dispatchScheduler;

        protected override void Context()
        {
            _dispatchScheduler = new SynchronousDispatchScheduler(_dispatchCommits, _persistStreams);
            _dispatchScheduler.Start();
        }

        protected override void Because()
        {
            _dispatchScheduler.ScheduleDispatch(_commit);
        }

        [Fact]
        public void should_provide_the_commit_to_the_dispatcher()
        {
            A.CallTo(() => _dispatchCommits.Dispatch(_commit)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_mark_the_commit_as_dispatched()
        {
            A.CallTo(() => _persistStreams.MarkCommitAsDispatched(_commit)).MustHaveHappened(Repeated.Exactly.Once);
        }
    }

    public class when_disposing_the_synchronous_dispatch_scheduler : SpecificationBase
    {
        private readonly IDispatchCommits _dispatchCommits = A.Fake<IDispatchCommits>();
        private readonly IPersistStreams _persistStreams = A.Fake<IPersistStreams>();
        private SynchronousDispatchScheduler _dispatchScheduler;

        protected override void Context()
        {
            _dispatchScheduler = new SynchronousDispatchScheduler(_dispatchCommits, _persistStreams);
        }

        protected override void Because()
        {
            _dispatchScheduler.Dispose();
            _dispatchScheduler.Dispose();
        }

        [Fact]
        public void should_dispose_the_underlying_dispatcher_exactly_once()
        {
            A.CallTo(() => _dispatchCommits.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_dispose_the_underlying_persistence_infrastructure_exactly_once()
        {
            A.CallTo(() => _persistStreams.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}