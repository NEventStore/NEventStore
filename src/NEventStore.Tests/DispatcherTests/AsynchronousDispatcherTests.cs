
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore.DispatcherTests
{
    using System.Threading;

    using FakeItEasy;

    using NEventStore.Dispatcher;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;

    public class when_instantiating_the_asynchronous_dispatch_scheduler : SpecificationBase
    {
        private readonly IDispatchCommits dispatcher = A.Fake<IDispatchCommits>();
        private readonly IPersistStreams persistence = A.Fake<IPersistStreams>();
        private ICommit[] _commits;
        private ICommit firstCommit, lastCommit;

        protected override void Context()
        {
            _commits = new[]
            {
                firstCommit = CommitHelper.Create(),
                lastCommit = CommitHelper.Create()
            };

            A.CallTo(() => persistence.GetUndispatchedCommits()).Returns(_commits);
        }

        protected override void Because()
        {
            new AsynchronousDispatchScheduler(dispatcher, persistence);
        }

        [Fact]
        public void should_take_a_few_milliseconds_for_the_other_thread_to_execute()
        {
            Thread.Sleep(25); // just a precaution because we're doing async tests
        }

        [Fact]
        public void should_initialize_the_persistence_engine()
        {
            A.CallTo(() => persistence.Initialize()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_get_the_set_of_undispatched_commits()
        {
            A.CallTo(() => persistence.GetUndispatchedCommits()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_provide_the_commits_to_the_dispatcher()
        {
            A.CallTo(() => dispatcher.Dispatch(firstCommit)).MustHaveHappened();
            A.CallTo(() => dispatcher.Dispatch(lastCommit)).MustHaveHappened();
        }
    }

    public class when_asynchronously_scheduling_a_commit_for_dispatch : SpecificationBase
    {
        private readonly ICommit _commit = CommitHelper.Create();
        private readonly IDispatchCommits dispatcher = A.Fake<IDispatchCommits>();
        private readonly IPersistStreams persistence = A.Fake<IPersistStreams>();
        private AsynchronousDispatchScheduler dispatchScheduler;

        protected override void Context()
        {
            dispatchScheduler = new AsynchronousDispatchScheduler(dispatcher, persistence);
        }

        protected override void Because()
        {
            dispatchScheduler.ScheduleDispatch(_commit);
        }

        [Fact]
        public void should_take_a_few_milliseconds_for_the_other_thread_to_execute()
        {
            Thread.Sleep(25); // just a precaution because we're doing async tests
        }

        [Fact]
        public void should_provide_the_commit_to_the_dispatcher()
        {
            A.CallTo(() => dispatcher.Dispatch(_commit)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_mark_the_commit_as_dispatched()
        {
            A.CallTo(() => persistence.MarkCommitAsDispatched(_commit)).MustHaveHappened(Repeated.Exactly.Once);
        }
    }

    public class when_disposing_the_async_dispatch_scheduler : SpecificationBase
    {
        private readonly IDispatchCommits dispatcher = A.Fake<IDispatchCommits>();
        private readonly IPersistStreams persistence = A.Fake<IPersistStreams>();
        private AsynchronousDispatchScheduler dispatchScheduler;

        protected override void Context()
        {
            dispatchScheduler = new AsynchronousDispatchScheduler(dispatcher, persistence);
        }

        protected override void Because()
        {
            dispatchScheduler.Dispose();
            dispatchScheduler.Dispose();
        }

        [Fact]
        public void should_dispose_the_underlying_dispatcher_exactly_once()
        {
            A.CallTo(() => dispatcher.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_dispose_the_underlying_persistence_infrastructure_exactly_once()
        {
            A.CallTo(() => persistence.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169