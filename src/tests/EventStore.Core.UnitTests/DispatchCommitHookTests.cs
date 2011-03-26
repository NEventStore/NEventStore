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
		static readonly DispatchCommitHook hook = new DispatchCommitHook(dispatcher.Object);

		Establish context = () =>
			dispatcher.Setup(x => x.Dispatch(null));

		Because of = () =>
			hook.PostCommit(commit);

		It should_invoke_the_configured_dispatcher = () =>
			dispatcher.Verify(x => x.Dispatch(commit), Times.Once());
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169