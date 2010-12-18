namespace EventStore.Core.UnitTests
{
	using System.Collections.Generic;
	using System.Linq;
	using Persistence;

	internal static class ExtensionMethods
	{
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