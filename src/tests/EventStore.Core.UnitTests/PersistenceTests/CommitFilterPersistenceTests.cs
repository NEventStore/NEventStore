#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests.PersistenceTests
{
	using System;
	using System.Linq;
	using Machine.Specifications;
	using Moq;
	using Persistence;
	using It = Machine.Specifications.It;

	[Subject("CommitFilterPersistence")]
	public class when_initializing_storage : using_persistence_infrastructure
	{
		Establish context = () =>
			fakePersistence.Setup(x => x.Initialize());

		Because of = () =>
			filterPersistence.Initialize();

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.Initialize(), Times.Exactly(1));
	}

	[Subject("CommitFilterPersistence")]
	public class when_reading_commits_from_a_revision : using_persistence_infrastructure
	{
		const int MinRevision = 42;
		private static readonly Commit[] commits = new[]
		{
			new Commit(streamId, Guid.NewGuid(), 0, 0, null, null, null),
			new Commit(streamId, Guid.NewGuid(), 0, 0, null, null, null)
		};
		static Mock<IFilterCommits<Commit>> readFilter;
		static Commit[] read;

		Establish context = () =>
		{
			fakePersistence.Setup(x => x.GetFrom(streamId, MinRevision)).Returns(commits);

			readFilter = new Mock<IFilterCommits<Commit>>();
			readFilter.Setup(x => x.Filter(commits.First())).Returns(commits.First());
			readFilter.Setup(x => x.Filter(commits.Last())).Returns((Commit)null);

			filterPersistence = new CommitFilterPersistence(fakePersistence.Object, readFilter.Object, null);
		};

		Because of = () =>
			read = filterPersistence.GetFrom(streamId, MinRevision).ToArray();

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.GetFrom(streamId, MinRevision), Times.Exactly(1));

		It should_pass_the_commits_through_the_filter = () =>
			readFilter.VerifyAll();

		It should_only_return_non_null_filtered_commits = () =>
			read.Length.ShouldEqual(1);
	}

	[Subject("CommitFilterPersistence")]
	public class when_reading_commits_until_a_revision : using_persistence_infrastructure
	{
		const int MaxRevision = 12;
		private static readonly Commit[] commits = new[]
		{
			new Commit(streamId, Guid.NewGuid(), 0, 0, null, null, null),
			new Commit(streamId, Guid.NewGuid(), 0, 0, null, null, null)
		};
		static Mock<IFilterCommits<Commit>> readFilter;
		static Commit[] read;

		Establish context = () =>
		{
			fakePersistence.Setup(x => x.GetUntil(streamId, MaxRevision)).Returns(commits);

			readFilter = new Mock<IFilterCommits<Commit>>();
			readFilter.Setup(x => x.Filter(commits.First())).Returns(commits.First());
			readFilter.Setup(x => x.Filter(commits.Last())).Returns((Commit)null);

			filterPersistence = new CommitFilterPersistence(fakePersistence.Object, readFilter.Object, null);
		};

		Because of = () =>
			read = filterPersistence.GetUntil(streamId, MaxRevision).ToArray();

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.GetUntil(streamId, MaxRevision), Times.Exactly(1));

		It should_pass_the_commits_through_the_filter = () =>
			readFilter.VerifyAll();

		It should_only_return_non_null_filtered_commits = () =>
			read.Length.ShouldEqual(1);
	}

	[Subject("CommitFilterPersistence")]
	public class when_persisting_an_attempt : using_persistence_infrastructure
	{
		static readonly CommitAttempt attempt = new CommitAttempt();
		static readonly CommitAttempt filtered = new CommitAttempt();
		static Mock<IFilterCommits<CommitAttempt>> writeFilter;

		Establish context = () =>
		{
			writeFilter = new Mock<IFilterCommits<CommitAttempt>>();
			writeFilter.Setup(x => x.Filter(attempt)).Returns(filtered);
			fakePersistence.Setup(x => x.Persist(filtered));

			filterPersistence = new CommitFilterPersistence(
				fakePersistence.Object, null, writeFilter.Object);
		};
			
		Because of = () =>
			filterPersistence.Persist(attempt);

		It should_provide_the_filtered_attempt_to_the_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.Persist(filtered), Times.Exactly(1));
	}

	[Subject("CommitFilterPersistence")]
	public class when_retreiving_undispatched_commits : using_persistence_infrastructure
	{
		Establish context = () =>
			fakePersistence.Setup(x => x.GetUndispatchedCommits());

		Because of = () =>
			filterPersistence.GetUndispatchedCommits();

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.GetUndispatchedCommits(), Times.Exactly(1));
	}

	[Subject("CommitFilterPersistence")]
	public class when_marking_a_commit_as_dispatched : using_persistence_infrastructure
	{
		static readonly Commit commit = new Commit(streamId, Guid.NewGuid(), 0, 0, null, null, null);

		Establish context = () =>
			fakePersistence.Setup(x => x.MarkCommitAsDispatched(commit));

		Because of = () =>
			filterPersistence.MarkCommitAsDispatched(commit);

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.MarkCommitAsDispatched(commit), Times.Exactly(1));
	}

	[Subject("CommitFilterPersistence")]
	public class when_retreiving_streams_to_snapshot : using_persistence_infrastructure
	{
		const int Threshold = 10;

		Establish context = () =>
			fakePersistence.Setup(x => x.GetStreamsToSnapshot(Threshold));

		Because of = () =>
			filterPersistence.GetStreamsToSnapshot(Threshold);

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.GetStreamsToSnapshot(Threshold), Times.Exactly(1));
	}

	[Subject("CommitFilterPersistence")]
	public class when_adding_a_snapshot : using_persistence_infrastructure
	{
		Establish context = () =>
			fakePersistence.Setup(x => x.AddSnapshot(streamId, 0, 1));

		Because of = () =>
			filterPersistence.AddSnapshot(streamId, 0, 1);

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.AddSnapshot(streamId, 0, 1), Times.Exactly(1));
	}

	public abstract class using_persistence_infrastructure
	{
		protected static Guid streamId = Guid.NewGuid();
		protected static Mock<IPersistStreams> fakePersistence;
		protected static CommitFilterPersistence filterPersistence;

		Establish context = () =>
		{
			fakePersistence = new Mock<IPersistStreams>();
			filterPersistence = new CommitFilterPersistence(fakePersistence.Object, null, null);
		};

		Cleanup everything = () =>
			streamId = Guid.NewGuid();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169