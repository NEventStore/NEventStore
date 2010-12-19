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

	public class when_asynchronously_dispatching_a_commit
	{
		static readonly Commit commit = new Commit(
			Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, null, null);
		static Mock<IPublishMessages> bus;
		static Mock<IPersistStreams> store;
		static AsynchronousDispatcher dispatcher;

		Establish context = () =>
		{
			bus = new Mock<IPublishMessages>();
			bus.Setup(x => x.Publish(commit));

			store = new Mock<IPersistStreams>();
			store.Setup(x => x.MarkCommitAsDispatched(commit));

			dispatcher = new AsynchronousDispatcher(bus.Object, store.Object, null);
		};

		Because of = () =>
			dispatcher.Dispatch(commit);

		It should_provide_the_commit_to_the_message_bus = () =>
			bus.Verify(x => x.Publish(commit), Times.Exactly(1));

		It should_mark_the_commit_as_dispatched = () =>
			store.Verify(x => x.MarkCommitAsDispatched(commit), Times.Exactly(1));
	}

	public class when_an_asynchronously_dispatch_commit_throws_an_exception
	{
		static readonly Commit commit = new Commit(
			Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, null, null);

		static AsynchronousDispatcher dispatcher;

		static Exception thrown;
		static Commit handedBack;

		Establish context = () =>
		{
			dispatcher = new AsynchronousDispatcher(
				null,
				null,
				(c, e) =>
				{
					handedBack = c;
					thrown = e;
				});
		};

		Because of = () =>
			dispatcher.Dispatch(commit);

		It should_handed_back_the_commit_that_caused_the_exception = () =>
			handedBack.ShouldEqual(commit);

		It should_provide_the_exception_that_indicates_the_problem = () =>
			thrown.ShouldNotBeNull();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169