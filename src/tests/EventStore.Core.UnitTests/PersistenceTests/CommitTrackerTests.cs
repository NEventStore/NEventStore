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
		static readonly Commit[] Commits = new[]
		{
			new Commit(StreamId, Guid.NewGuid(), 1, 1, null, null, null),
			new Commit(StreamId, Guid.NewGuid(), 2, 2, null, null, null),
			new Commit(StreamId, Guid.NewGuid(), 3, 3, null, null, null) // causes first commit to no longer tracked.
		};

		static CommitTracker tracker;
		
		Establish context = () =>
			tracker = new CommitTracker(MaxCommitsToTrackPerStream);

		Because of = () =>
		{
			foreach (var commit in Commits)
				tracker.Track(commit);
		};

		It should_only_contain_streams_explicitly_tracked = () => tracker.Contains(new CommitAttempt
		{
			StreamId = Guid.Empty, // non-existant partition in tracker
			CommitId = Commits[0].CommitId
		}).ShouldBeFalse();

		It should_find_tracked_commits = () =>
		{
			var stillTracked = new CommitAttempt
			{
				StreamId = Commits.Last().StreamId,
				CommitId = Commits.Last().CommitId
			};

			tracker.Contains(stillTracked).ShouldBeTrue();
		};

		It should_only_track_the_specified_number_of_commits = () =>
		{
			var droppedFromTracking = new CommitAttempt
			{
				StreamId = Commits.First().StreamId,
				CommitId = Commits.First().CommitId
			};

			tracker.Contains(droppedFromTracking).ShouldBeFalse();
		};
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169