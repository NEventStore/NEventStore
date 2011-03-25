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
	public class when_initializing_storage : using_mock_persistence
	{
		Establish context = () =>
			fakePersistence.Setup(x => x.Initialize());

		Because of = () =>
			filterPersistence.Initialize();

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.Initialize(), Times.Once());
	}

	[Subject("CommitFilterPersistence")]
	public class when_reading_commits_for_a_given_stream : using_mock_persistence
	{
		const int MinRevision = 42; // doesn't matter in this test
		const int MaxRevision = 43; // doesn't matter in this test
		private static readonly Commit[] commits = new[]
		{
			new Commit(streamId, 0, Guid.NewGuid(), 0, DateTime.UtcNow, null, null),
			new Commit(streamId, 0, Guid.NewGuid(), 0, DateTime.UtcNow, null, null)
		};
		static Mock<IFilterCommitReads> readFilter;
		static Commit[] read;

		Establish context = () =>
		{
			fakePersistence.Setup(x => x.GetFrom(streamId, MinRevision, MaxRevision)).Returns(commits);

			readFilter = new Mock<IFilterCommitReads>();
			readFilter.Setup(x => x.FilterRead(commits.First())).Returns(commits.First());
			readFilter.Setup(x => x.FilterRead(commits.Last())).Returns((Commit)null);

			filterPersistence = new CommitFilterPersistence(
				fakePersistence.Object,
				new[] { readFilter.Object },
				null);
		};

		Because of = () =>
			read = filterPersistence.GetFrom(streamId, MinRevision, MaxRevision).ToArray();

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.GetFrom(streamId, MinRevision, MaxRevision), Times.Once());

		It should_pass_the_commits_through_the_filter = () =>
			readFilter.VerifyAll();

		It should_only_return_non_null_filtered_commits = () =>
			read.Length.ShouldEqual(1);
	}

	[Subject("CommitFilterPersistence")]
	public class when_persisting_an_attempt : using_mock_persistence
	{
		static readonly Commit attempt = BuildCommitStub();
		static readonly Commit filtered = BuildCommitStub();
		static Mock<IFilterCommitWrites> writeFilter;

		Establish context = () =>
		{
			writeFilter = new Mock<IFilterCommitWrites>();
			writeFilter.Setup(x => x.FilterWrite(attempt)).Returns(filtered);
			fakePersistence.Setup(x => x.Commit(filtered));

			filterPersistence = new CommitFilterPersistence(
				fakePersistence.Object, null, new[] { writeFilter.Object });
		};

		Because of = () =>
			filterPersistence.Commit(attempt);

		It should_provide_the_filtered_attempt_to_the_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.Commit(filtered), Times.Once());
	}

	[Subject("CommitFilterPersistence")]
	public class when_filtering_out_a_persistence_attempt : using_mock_persistence
	{
		static readonly Commit attempt = BuildCommitStub();
		static Mock<IFilterCommitWrites> writeFilter;
		static Commit persisted;

		Establish context = () =>
		{
			writeFilter = new Mock<IFilterCommitWrites>();
			writeFilter.Setup(x => x.FilterWrite(attempt)).Returns((Commit)null);
			fakePersistence.Setup(x => x.Commit(Moq.It.IsAny<Commit>()));

			filterPersistence = new CommitFilterPersistence(
				fakePersistence.Object, null, new[] { writeFilter.Object });
		};

		Because of = () =>
			persisted = filterPersistence.Commit(attempt);

		It skip_providing_the_commit_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.Commit(Moq.It.IsAny<Commit>()), Times.Never());

		It should_return_null_to_the_caller = () =>
			persisted.ShouldBeNull();
	}

	[Subject("CommitFilterPersistence")]
	public class when_retreiving_undispatched_commits : using_mock_persistence
	{
		Establish context = () =>
			fakePersistence.Setup(x => x.GetUndispatchedCommits());

		Because of = () =>
			filterPersistence.GetUndispatchedCommits();

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.GetUndispatchedCommits(), Times.Once());
	}

	[Subject("CommitFilterPersistence")]
	public class when_marking_a_commit_as_dispatched : using_mock_persistence
	{
		static readonly Commit commit = new Commit(streamId, 0, Guid.NewGuid(), 0, DateTime.UtcNow, null, null);

		Establish context = () =>
			fakePersistence.Setup(x => x.MarkCommitAsDispatched(commit));

		Because of = () =>
			filterPersistence.MarkCommitAsDispatched(commit);

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.MarkCommitAsDispatched(commit), Times.Once());
	}

	[Subject("CommitFilterPersistence")]
	public class when_retreiving_list_of_streams_to_snapshot : using_mock_persistence
	{
		const int Threshold = 10;

		Establish context = () =>
			fakePersistence.Setup(x => x.GetStreamsToSnapshot(Threshold));

		Because of = () =>
			filterPersistence.GetStreamsToSnapshot(Threshold);

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.GetStreamsToSnapshot(Threshold), Times.Once());
	}

	[Subject("CommitFilterPersistence")]
	public class when_adding_a_snapshot : using_mock_persistence
	{
		static readonly Snapshot Snapshot = new Snapshot(streamId, 0, 1);

		Establish context = () =>
			fakePersistence.Setup(x => x.AddSnapshot(Snapshot));

		Because of = () =>
			filterPersistence.AddSnapshot(Snapshot);

		It should_call_to_the_underlying_persistence_infrastructure = () =>
			fakePersistence.Verify(x => x.AddSnapshot(Snapshot), Times.Once());
	}

	[Subject("CommitFilterPersistence")]
	public class when_being_disposed : using_mock_persistence
	{
		Establish context = () =>
			fakePersistence.Setup(x => x.Dispose());

		Because of = () =>
		{
			filterPersistence.Dispose();
			filterPersistence.Dispose();
		};

		It should_call_dispose_on_the_underlying_persistence_infrastructure_exactly_once = () =>
			fakePersistence.Verify(x => x.Dispose(), Times.Once());
	}

	public abstract class using_mock_persistence
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

		protected static Commit BuildCommitStub()
		{
			return new Commit(streamId, 1, Guid.NewGuid(), 1, DateTime.UtcNow, null, null);
		}
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169