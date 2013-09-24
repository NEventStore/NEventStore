
#pragma warning disable 169

namespace NEventStore
{
    using System;
    using Moq;
    using NEventStore.Dispatcher;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class when_a_commit_has_been_persisted : SpecificationBase
    {
        private readonly ICommit _commit = new Commit(Bucket.Default, Guid.NewGuid().ToString(), 0, Guid.NewGuid(), 0, DateTime.MinValue, new LongCheckpoint(0).Value, null, null);

        private readonly Mock<IScheduleDispatches> _dispatcher = new Mock<IScheduleDispatches>();
        private DispatchSchedulerPipelineHook _dispatchSchedulerHook;

        protected override void Context()
        {
            _dispatchSchedulerHook = new DispatchSchedulerPipelineHook(_dispatcher.Object);
            _dispatcher.Setup(x => x.ScheduleDispatch(null));
        }

        protected override void Because()
        {
            _dispatchSchedulerHook.PostCommit(_commit);
        }

        [Fact]
        public void should_invoke_the_configured_dispatcher()
        {
            _dispatcher.Verify(x => x.ScheduleDispatch(_commit), Times.Once());
        }
    }

    public class when_the_hook_has_no_dispatcher_configured : SpecificationBase
    {
        private readonly DispatchSchedulerPipelineHook _dispatchSchedulerHook = new DispatchSchedulerPipelineHook();

        private readonly ICommit _commit = new Commit(Bucket.Default, Guid.NewGuid().ToString(), 0, Guid.NewGuid(), 0, DateTime.MinValue, new LongCheckpoint(0).Value, null, null);

        private Exception _thrown;

        protected override void Because()
        {
            _thrown = Catch.Exception(() => _dispatchSchedulerHook.PostCommit(_commit));
        }

        [Fact]
        public void should_not_throw_an_exception()
        {
            _thrown.ShouldBeNull();
        }
    }

    public class when_a_commit_is_selected : SpecificationBase
    {
        private readonly DispatchSchedulerPipelineHook _dispatchSchedulerHook = new DispatchSchedulerPipelineHook();

        private readonly ICommit _commit = new Commit(Bucket.Default, Guid.NewGuid().ToString(), 0, Guid.NewGuid(), 0, DateTime.MinValue, new LongCheckpoint(0).Value, null, null);

        private ICommit _selected;

        protected override void Because()
        {
            _selected = _dispatchSchedulerHook.Select(_commit);
        }

        [Fact]
        public void should_always_return_the_exact_same_commit()
        {
            ReferenceEquals(_selected, _commit).ShouldBeTrue();
        }
    }
}

#pragma warning restore 169