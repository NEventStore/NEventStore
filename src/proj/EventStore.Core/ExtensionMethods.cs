namespace EventStore.Core
{
	using System.Collections.Generic;

	internal static class ExtensionMethods
	{
		public static void AddEventsOrClearOnSnapshot(this ICollection<object> events, Commit commit)
		{
			if (commit.Snapshot != null)
				events.Clear();
			else
				events.AddEvents(commit);
		}
		public static void AddEvents(this ICollection<object> @events, Commit commit)
		{
			foreach (var @event in commit.Events)
				events.Add(@event.Body);
		}
	}
}