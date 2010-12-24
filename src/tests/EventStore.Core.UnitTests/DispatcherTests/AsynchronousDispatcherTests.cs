#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests.DispatcherTests
{
	using System;
	using System.Linq;
	using System.Threading;
	using Dispatcher;
	using Machine.Specifications;
	using Moq;
	using Persistence;
	using It = Machine.Specifications.It;

	[Subject("AsynchronousDispatcher")]
	public class when_instantiaing_the_asynchronous_dispatcher
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
			new AsynchronousDispatcher(bus.Object, persistence.Object, null);

		It should_take_a_few_milliseconds_for_the_other_thread_to_execute = () =>
			Thread.Sleep(10); // just a precaution because we're doing async tests

		It should_get_the_set_of_undispatched_commits = () =>
			persistence.Verify(x => x.GetUndispatchedCommits(), Times.Exactly(1));

		It should_provide_the_commits_to_the_publisher = () =>
			bus.VerifyAll();
	}

	[Subject("AsynchronousDispatcher")]
	public class when_asynchronously_dispatching_a_commit
	{
		static readonly Commit commit = new Commit(
			Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, null, null);
		static readonly Mock<IPublishMessages> bus = new Mock<IPublishMessages>();
		static readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
		static AsynchronousDispatcher dispatcher;

		Establish context = () =>
		{
			bus.Setup(x => x.Publish(commit));
			persistence.Setup(x => x.MarkCommitAsDispatched(commit));

			dispatcher = new AsynchronousDispatcher(bus.Object, persistence.Object, null);
		};

		Because of = () =>
			dispatcher.Dispatch(commit);

		It should_take_a_few_milliseconds_for_the_other_thread_to_execute = () =>
			Thread.Sleep(10); // just a precaution because we're doing async tests

		It should_provide_the_commit_to_the_message_bus = () =>
			bus.Verify(x => x.Publish(commit), Times.Exactly(1));

		It should_mark_the_commit_as_dispatched = () =>
			persistence.Verify(x => x.MarkCommitAsDispatched(commit), Times.Exactly(1));
	}

	[Subject("AsynchronousDispatcher")]
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
				null, // we want a NullReferenceException to be thrown
				new Mock<IPersistStreams>().Object,
				(c, e) =>
				{
					handedBack = c;
					thrown = e;
				});
		};

		Because of = () =>
			dispatcher.Dispatch(commit);

		It should_take_a_few_milliseconds_for_the_other_thread_to_execute = () =>
			Thread.Sleep(10); // just a precaution because we're doing async tests

		It should_handed_back_the_commit_that_caused_the_exception = () =>
			handedBack.ShouldEqual(commit);

		It should_provide_the_exception_that_indicates_the_problem = () =>
			thrown.ShouldNotBeNull();
	}

	[Subject("AsynchronousDispatcher")]
	public class when_disposing_the_async_dispatcher
	{
		static readonly Mock<IPublishMessages> bus = new Mock<IPublishMessages>();
		static readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
		static AsynchronousDispatcher dispatcher;

		Establish context = () =>
		{
			bus.Setup(x => x.Dispose());
			persistence.Setup(x => x.Dispose());
			dispatcher = new AsynchronousDispatcher(bus.Object, persistence.Object, null);
		};

		Because of = () =>
		{
			dispatcher.Dispose();
			dispatcher.Dispose();
		};

		It should_dispose_the_underlying_message_bus_exactly_once = () =>
			bus.Verify(x => x.Dispose(), Times.Exactly(1));

		It should_dispose_the_underlying_persistence_infrastructure_exactly_once = () =>
			bus.Verify(x => x.Dispose(), Times.Exactly(1));
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169