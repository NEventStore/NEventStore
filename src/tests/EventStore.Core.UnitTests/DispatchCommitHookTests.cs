#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using Dispatcher;
	using Machine.Specifications;
	using Moq;
	using It = Machine.Specifications.It;

	[Subject("DispatchCommitHook")]
	public class when_a_commit_has_been_persisted
	{
		static readonly Commit commit = new Commit(Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);
		static readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
		static readonly DispatchPipelineHook PipelineHook = new DispatchPipelineHook(dispatcher.Object);

		Establish context = () =>
			dispatcher.Setup(x => x.Dispatch(null));

		Because of = () =>
			PipelineHook.PostCommit(commit);

		It should_invoke_the_configured_dispatcher = () =>
			dispatcher.Verify(x => x.Dispatch(commit), Times.Once());
	}

	[Subject("DispatchCommitHook")]
	public class when_the_hook_has_no_dispatcher_configured
	{
		static readonly Commit commit = new Commit(Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);
		static readonly DispatchPipelineHook PipelineHook = new DispatchPipelineHook();
		static Exception thrown;

		Because of = () =>
			thrown = Catch.Exception(() => PipelineHook.PostCommit(commit));

		It should_not_throw_an_exception = () =>
			thrown.ShouldBeNull();
	}

	[Subject("DispatchCommitHook")]
	public class when_a_commit_is_selected
	{
		static readonly Commit commit = new Commit(Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);
		static readonly DispatchPipelineHook PipelineHook = new DispatchPipelineHook();
		static Commit selected;

		Because of = () =>
			selected = PipelineHook.Select(commit);

		It should_always_return_the_exact_same_commit = () =>
			object.ReferenceEquals(selected, commit).ShouldBeTrue();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169