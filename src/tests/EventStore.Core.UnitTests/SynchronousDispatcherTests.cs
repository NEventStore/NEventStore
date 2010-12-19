#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
	using System;
	using Dispatcher;
	using Machine.Specifications;
	using Moq;
	using Persistence;
	using It = Machine.Specifications.It;

	public class when_synchronously_dispatching_a_commit
	{
		static readonly Commit commit = new Commit(
			Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, null, null);
		static Mock<IPublishMessages> bus;
		static Mock<IPersistStreams> store;
		private static SynchronousDispatcher dispatcher;

		Establish context = () =>
		{
			bus = new Mock<IPublishMessages>();
			bus.Setup(x => x.Publish(commit));

			store = new Mock<IPersistStreams>();
			store.Setup(x => x.MarkCommitAsDispatched(commit));

			dispatcher = new SynchronousDispatcher(bus.Object, store.Object);
		};

		Because of = () =>
			dispatcher.Dispatch(commit);

		It should_provide_the_commit_to_the_message_bus = () =>
			bus.Verify(x => x.Publish(commit), Times.Exactly(1));

		It should_mark_the_commit_as_dispatched = () =>
			store.Verify(x => x.MarkCommitAsDispatched(commit), Times.Exactly(1));
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169