using EventStore.Persistence.AcceptanceTests.BDD;
using Xunit;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests.DispatcherTests
{
    using System;
    using System.Linq;
    using System.Threading;
    using Dispatcher;
    using Moq;
    using Persistence;

    public class when_instantiating_the_asynchronous_dispatch_scheduler : SpecificationBase
    {
        readonly Guid streamId = Guid.NewGuid();
        Commit[] commits;
        readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
        readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();

        protected override void Context()
        {
            commits = new[]
            {
			    new Commit(streamId, 0, Guid.NewGuid(), 0, SystemTime.UtcNow, null, null),
			    new Commit(streamId, 0, Guid.NewGuid(), 0, SystemTime.UtcNow, null, null)
		    };

            persistence.Setup(x => x.Initialize());
            persistence.Setup(x => x.GetUndispatchedCommits()).Returns(commits);
            dispatcher.Setup(x => x.Dispatch(commits.First()));
            dispatcher.Setup(x => x.Dispatch(commits.Last()));
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
        readonly Commit commit = new Commit(Guid.NewGuid(), 0, Guid.NewGuid(), 0, SystemTime.UtcNow, null, null);
        readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
        readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
        AsynchronousDispatchScheduler dispatchScheduler;

        protected override void Context()
        {
            dispatcher.Setup(x => x.Dispatch(commit));
            persistence.Setup(x => x.MarkCommitAsDispatched(commit));

            dispatchScheduler = new AsynchronousDispatchScheduler(dispatcher.Object, persistence.Object);
        }

        protected override void Because()
        {
            dispatchScheduler.ScheduleDispatch(commit);
        }

        [Fact]
        public void should_take_a_few_milliseconds_for_the_other_thread_to_execute()
        {
            Thread.Sleep(25); // just a precaution because we're doing async tests
        }

        [Fact]
        public void should_provide_the_commit_to_the_dispatcher()
        {
            dispatcher.Verify(x => x.Dispatch(commit), Times.Once());
        }

        [Fact]
        public void should_mark_the_commit_as_dispatched()
        {
            persistence.Verify(x => x.MarkCommitAsDispatched(commit), Times.Once());
        }
    }

    public class when_disposing_the_async_dispatch_scheduler : SpecificationBase
    {
        readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
        readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
        AsynchronousDispatchScheduler dispatchScheduler;

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