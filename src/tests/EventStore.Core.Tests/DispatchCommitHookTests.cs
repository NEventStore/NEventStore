
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.Tests
{
    using EventStore.Persistence.AcceptanceTests;
    using EventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;
    using System;
    using EventStore.Dispatcher;
    using Moq;

    public class when_a_commit_has_been_persisted : SpecificationBase
	{
        readonly Commit commit = new Commit(
			Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);

        readonly Mock<IScheduleDispatches> dispatcher = new Mock<IScheduleDispatches>();
        DispatchSchedulerPipelineHook DispatchSchedulerHook;

	    protected override void Context()
	    {
            DispatchSchedulerHook = new DispatchSchedulerPipelineHook(dispatcher.Object);
	        dispatcher.Setup(x => x.ScheduleDispatch(null));
	    }

	    protected override void Because()
	    {
	        DispatchSchedulerHook.PostCommit(commit);
	    }

        [Fact]
	    public void should_invoke_the_configured_dispatcher()
	    {
	        dispatcher.Verify(x => x.ScheduleDispatch(commit), Times.Once());
	    }
	}

    public class when_the_hook_has_no_dispatcher_configured : SpecificationBase
	{
        readonly Commit commit = new Commit(
			Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);

        readonly DispatchSchedulerPipelineHook DispatchSchedulerHook = new DispatchSchedulerPipelineHook();
		Exception thrown;

	    protected override void Because()
	    {
	        thrown = Catch.Exception(() => DispatchSchedulerHook.PostCommit(commit));
	    }

        [Fact]
        public void should_not_throw_an_exception()
	    {
	        thrown.ShouldBeNull();
	    }
	}

    public class when_a_commit_is_selected : SpecificationBase
	{
        readonly Commit commit = new Commit(
			Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);

        readonly DispatchSchedulerPipelineHook DispatchSchedulerHook = new DispatchSchedulerPipelineHook();
		Commit selected;

	    protected override void Because()
	    {
	        selected = DispatchSchedulerHook.Select(commit);
	    }

        [Fact]
        public void should_always_return_the_exact_same_commit()
	    {
	        ReferenceEquals(selected, commit).ShouldBeTrue();
	    }
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169