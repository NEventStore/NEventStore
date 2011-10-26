#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using Dispatcher;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject("DispatchSchedulerPipelinkHook")]
	public class when_a_commit_has_been_persisted
	{
		static readonly Commit commit = new Commit(
			Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);
		static readonly Mock<IScheduleDispatches> dispatcher = new Mock<IScheduleDispatches>();
		static readonly DispatchSchedulerPipelineHook DispatchSchedulerHook = new DispatchSchedulerPipelineHook(dispatcher.Object);

		Establish context = () =>
			dispatcher.Setup(x => x.ScheduleDispatch(null));

		Because of = () =>
			DispatchSchedulerHook.PostCommit(commit);

		It should_invoke_the_configured_dispatcher = () =>
			dispatcher.Verify(x => x.ScheduleDispatch(commit), Times.Once());
	}

	[Subject("DispatchSchedulerPipelinkHook")]
	public class when_the_hook_has_no_dispatcher_configured
	{
		static readonly Commit commit = new Commit(
			Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);
		static readonly DispatchSchedulerPipelineHook DispatchSchedulerHook = new DispatchSchedulerPipelineHook();
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => DispatchSchedulerHook.PostCommit(commit));

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject("DispatchSchedulerPipelinkHook")]
	public class when_a_commit_is_selected
	{
		static readonly Commit commit = new Commit(
			Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);
		static readonly DispatchSchedulerPipelineHook DispatchSchedulerHook = new DispatchSchedulerPipelineHook();
		static Commit selected;

		Because of = () =>
			selected = DispatchSchedulerHook.Select(commit);

		It should_always_return_the_exact_same_commit = () =>
			ReferenceEquals(selected, commit).ShouldBeTrue();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169