#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests.PersistenceTests
{
	using System;
	using System.Linq;
	using Machine.Specifications;
	using Persistence;
	using It = Machine.Specifications.It;

	[Subject("CommitTracker")]
	public class when_tracking_commits
	{
		const int MaxCommitsToTrackPerStream = 2;
		static readonly Guid StreamId = Guid.NewGuid();
		static readonly Commit[] TrackedCommits = new[]
		{
			BuildCommit(StreamId, Guid.NewGuid()),
			BuildCommit(StreamId, Guid.NewGuid()),
			BuildCommit(StreamId, Guid.NewGuid())
		};

		static CommitTracker tracker;

		Establish context = () =>
			tracker = new CommitTracker(MaxCommitsToTrackPerStream);

		Because of = () =>
		{
			foreach (var commit in TrackedCommits)
				tracker.Track(commit);
		};

		It should_only_contain_streams_explicitly_tracked = () =>
		{
			var untracked = BuildCommit(Guid.Empty, TrackedCommits[0].CommitId);
			tracker.Contains(untracked).ShouldBeFalse();
		};

		It should_find_tracked_commits = () =>
		{
			var stillTracked = BuildCommit(TrackedCommits.Last().StreamId, TrackedCommits.Last().CommitId);
			tracker.Contains(stillTracked).ShouldBeTrue();
		};

		It should_only_track_the_specified_number_of_commits = () =>
		{
			var droppedFromTracking = BuildCommit(
				TrackedCommits.First().StreamId, TrackedCommits.First().CommitId);
			tracker.Contains(droppedFromTracking).ShouldBeFalse();
		};

		private static Commit BuildCommit(Guid streamId, Guid commitId)
		{
			return new Commit(streamId, 0, commitId, 0, null, null);
		}
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169