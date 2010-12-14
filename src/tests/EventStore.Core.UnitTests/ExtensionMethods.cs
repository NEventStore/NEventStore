namespace EventStore.Core.UnitTests
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	internal static class ExtensionMethods
	{
		public static Commit BuildCommit(this Guid streamId, object messageBody, object snapshot)
		{
			return new Commit(streamId, Guid.NewGuid(), 1, 1, null, null, snapshot)
			{
				Events = { new EventMessage { Body = messageBody } }
			};
		}
		public static Commit Add(this Commit commit, object messageBody, object snapshot)
		{
			commit = new Commit(
				commit.StreamId,
				commit.CommitId,
				commit.StreamRevision,
				commit.CommitSequence,
				commit.Headers,
				commit.Events,
				snapshot);

			commit.Events.Add(new EventMessage { Body = messageBody });

			return commit;
		}

		public static long MostRecentRevision(this IEnumerable<Commit> commits)
		{
			return commits.Last().StreamRevision;
		}
		public static object MostRecentSnapshot(this IEnumerable<Commit> commits)
		{
			return commits.Where(x => x.Snapshot != null).Last().Snapshot;
		}
		public static object OldestEvent(this IEnumerable<Commit> commits)
		{
			return commits.First().Events.First().Body;
		}
		public static object NewestEvent(this IEnumerable<Commit> commits)
		{
			return commits.Last().Events.Last().Body;
		}
		public static int CountEvents(this IEnumerable<Commit> commits)
		{
			return commits.Sum(x => x.Events.Count);
		}
	}
}