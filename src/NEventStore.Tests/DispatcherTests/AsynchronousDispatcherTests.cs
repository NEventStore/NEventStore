
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore.DispatcherTests
{
    using System.Linq;
    using System.Threading;
    using Moq;
    using NEventStore.Dispatcher;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;

    public class when_instantiating_the_asynchronous_dispatch_scheduler : SpecificationBase
    {
        private readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
        private readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
        private ICommit[] _commits;

        protected override void Context()
        {
            _commits = new[]
            {
                CommitHelper.Create(),
                CommitHelper.Create()
            };

            persistence.Setup(x => x.Initialize());
            persistence.Setup(x => x.GetUndispatchedCommits()).Returns(_commits);
            dispatcher.Setup(x => x.Dispatch(_commits.First()));
            dispatcher.Setup(x => x.Dispatch(_commits.Last()));
        }

        protected override void Because()
        {
            new AsynchronousDispatchScheduler(dispatcher.Object, persistence.Object);
        }

        [Fact]
        public void should_take_a_few_milliseconds_for_the_other_thread_to_execute()
        {
            Thread.Sleep(25); // just a precaution because we're doing async tests
        }

        [Fact]
        public void should_initialize_the_persistence_engine()
        {
            persistence.Verify(x => x.Initialize(), Times.Once());
        }

        [Fact]
        public void should_get_the_set_of_undispatched_commits()
        {
            persistence.Verify(x => x.GetUndispatchedCommits(), Times.Once());
        }

        [Fact]
        public void should_provide_the_commits_to_the_dispatcher()
        {
            dispatcher.VerifyAll();
        }
    }

    public class when_asynchronously_scheduling_a_commit_for_dispatch : SpecificationBase
    {
        private readonly ICommit _commit = CommitHelper.Create();
        private readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
        private readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
        private AsynchronousDispatchScheduler dispatchScheduler;

        protected override void Context()
        {
            dispatcher.Setup(x => x.Dispatch(_commit));
            persistence.Setup(x => x.MarkCommitAsDispatched(_commit));

            dispatchScheduler = new AsynchronousDispatchScheduler(dispatcher.Object, persistence.Object);
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
            dispatcher.Verify(x => x.Dispatch(_commit), Times.Once());
        }

        [Fact]
        public void should_mark_the_commit_as_dispatched()
        {
            persistence.Verify(x => x.MarkCommitAsDispatched(_commit), Times.Once());
        }
    }

    public class when_disposing_the_async_dispatch_scheduler : SpecificationBase
    {
        private readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
        private readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
        private AsynchronousDispatchScheduler dispatchScheduler;

        protected override void Context()
        {
            dispatcher.Setup(x => x.Dispose());
            persistence.Setup(x => x.Dispose());
            dispatchScheduler = new AsynchronousDispatchScheduler(dispatcher.Object, persistence.Object);
        }

        protected override void Because()
        {
            dispatchScheduler.Dispose();
            dispatchScheduler.Dispose();
        }

        [Fact]
        public void should_dispose_the_underlying_dispatcher_exactly_once()
        {
            dispatcher.Verify(x => x.Dispose(), Times.Once());
        }

        [Fact]
        public void should_dispose_the_underlying_persistence_infrastructure_exactly_once()
        {
            dispatcher.Verify(x => x.Dispose(), Times.Once());
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169