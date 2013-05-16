#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests.DispatcherTests
{
	using System;
	using System.Linq;
	using Dispatcher;
	using Machine.Specifications;
	using Moq;
	using EventStore.Persistence;
	using It = Machine.Specifications.It;

	[Subject("SynchronousDispatchScheduler")]
	public class when_instantiating_the_synchronous_dispatch_scheduler
	{
		static readonly Guid streamId = Guid.NewGuid();
		private static readonly Commit[] commits =
		{
			new Commit(streamId, 0, Guid.NewGuid(), 0, SystemTime.UtcNow, null, null),
			new Commit(streamId, 0, Guid.NewGuid(), 0, SystemTime.UtcNow, null, null)
		};
		static readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
		static readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();

		Establish context = () =>
		{
			persistence.Setup(x => x.Initialize());
			persistence.Setup(x => x.GetUndispatchedCommits()).Returns(commits);
			dispatcher.Setup(x => x.Dispatch(commits.First()));
			dispatcher.Setup(x => x.Dispatch(commits.Last()));
		};

		Because of = () =>
			new SynchronousDispatchScheduler(dispatcher.Object, persistence.Object);

		It should_initialize_the_persistence_engine = () =>
			persistence.Verify(x => x.Initialize(), Times.Once());

		It should_get_the_set_of_undispatched_commits = () =>
			persistence.Verify(x => x.GetUndispatchedCommits(), Times.Once());

		It should_provide_the_commits_to_the_dispatcher = () =>
			dispatcher.VerifyAll();
	}

	[Subject("SynchronousDispatchScheduler")]
	public class when_synchronously_scheduling_a_commit_for_dispatch
	{
		static readonly Commit commit = new Commit(Guid.NewGuid(), 0, Guid.NewGuid(), 0, SystemTime.UtcNow, null, null);
		static readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
		static readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
		static SynchronousDispatchScheduler dispatchScheduler;

		Establish context = () =>
		{
			dispatcher.Setup(x => x.Dispatch(commit));
			persistence.Setup(x => x.MarkCommitAsDispatched(commit));

			dispatchScheduler = new SynchronousDispatchScheduler(dispatcher.Object, persistence.Object);
		};

		Because of = () =>
			dispatchScheduler.ScheduleDispatch(commit);

		It should_provide_the_commit_to_the_dispatcher = () =>
			dispatcher.Verify(x => x.Dispatch(commit), Times.Once());

		It should_mark_the_commit_as_dispatched = () =>
			persistence.Verify(x => x.MarkCommitAsDispatched(commit), Times.Once());
	}

	[Subject("SynchronousDispatchScheduler")]
	public class when_disposing_the_synchronous_dispatch_scheduler
	{
		static readonly Mock<IDispatchCommits> dispatcher = new Mock<IDispatchCommits>();
		static readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
		static SynchronousDispatchScheduler dispatchScheduler;

		Establish context = () =>
		{
			dispatcher.Setup(x => x.Dispose());
			persistence.Setup(x => x.Dispose());
			dispatchScheduler = new SynchronousDispatchScheduler(dispatcher.Object, persistence.Object);
		};

		Because of = () =>
		{
			dispatchScheduler.Dispose();
			dispatchScheduler.Dispose();
		};

		It should_dispose_the_underlying_dispatcher_exactly_once = () =>
			dispatcher.Verify(x => x.Dispose(), Times.Once());

		It should_dispose_the_underlying_persistence_infrastructure_exactly_once = () =>
			dispatcher.Verify(x => x.Dispose(), Times.Once());
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169