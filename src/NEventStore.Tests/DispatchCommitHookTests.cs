
#pragma warning disable 169

namespace NEventStore
{
    using System;
    using FakeItEasy;
    using FluentAssertions;
    using NEventStore.Dispatcher;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;

    public class when_a_commit_has_been_persisted : SpecificationBase
    {
        private readonly ICommit _commit = new Commit(Bucket.Default, Guid.NewGuid().ToString(), 0, Guid.NewGuid(), 0, DateTime.MinValue, new LongCheckpoint(0).Value, null, null);
        
        private readonly IScheduleDispatches _dispatcher = A.Fake<IScheduleDispatches>();

        private DispatchSchedulerPipelineHook _dispatchSchedulerHook;

        protected override void Context()
        {
            _dispatchSchedulerHook = new DispatchSchedulerPipelineHook(_dispatcher);
        }

        protected override void Because()
        {
            _dispatchSchedulerHook.PostCommit(_commit);
        }

        [Fact]
        public void should_invoke_the_configured_dispatcher()
        {
            A.CallTo(() => _dispatcher.ScheduleDispatch(_commit)).MustHaveHappened(Repeated.Exactly.Once);
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
            _thrown.Should().BeNull();
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
            ReferenceEquals(_selected, _commit).Should().BeTrue();
        }
    }
}

#pragma warning restore 169