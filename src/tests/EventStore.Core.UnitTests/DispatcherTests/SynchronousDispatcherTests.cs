#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests.DispatcherTests
{
	using System;
	using System.Linq;
	using Dispatcher;
	using Machine.Specifications;
	using Moq;
	using Persistence;
	using It = Machine.Specifications.It;

	public class when_instantiaing_the_synchronous_dispatcher
	{
		static readonly Guid streamId = Guid.NewGuid();
		private static readonly Commit[] commits =
		{
			new Commit(streamId, Guid.NewGuid(), 0, 0, null, null, null),
			new Commit(streamId, Guid.NewGuid(), 0, 0, null, null, null)
		};
		static readonly Mock<IPublishMessages> bus = new Mock<IPublishMessages>();
		static readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();

		Establish context = () =>
		{
			persistence.Setup(x => x.GetUndispatchedCommits()).Returns(commits);
			bus.Setup(x => x.Publish(commits.First()));
			bus.Setup(x => x.Publish(commits.Last()));
		};

		Because of = () =>
			new SynchronousDispatcher(bus.Object, persistence.Object);

		It should_get_the_set_of_undispatched_commits = () =>
			persistence.Verify(x => x.GetUndispatchedCommits(), Times.Exactly(1));

		It should_provide_the_commits_to_the_publisher = () =>
			bus.VerifyAll();
	}

	public class when_synchronously_dispatching_a_commit
	{
		static readonly Commit commit = new Commit(
			Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, null, null);
		static readonly Mock<IPublishMessages> bus = new Mock<IPublishMessages>();
		static readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
		static SynchronousDispatcher dispatcher;

		Establish context = () =>
		{
			bus.Setup(x => x.Publish(commit));
			persistence.Setup(x => x.MarkCommitAsDispatched(commit));

			dispatcher = new SynchronousDispatcher(bus.Object, persistence.Object);
		};

		Because of = () =>
			dispatcher.Dispatch(commit);

		It should_provide_the_commit_to_the_message_bus = () =>
			bus.Verify(x => x.Publish(commit), Times.Exactly(1));

		It should_mark_the_commit_as_dispatched = () =>
			persistence.Verify(x => x.MarkCommitAsDispatched(commit), Times.Exactly(1));
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169