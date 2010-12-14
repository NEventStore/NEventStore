namespace EventStore.Core
{
	using System;
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

		public static bool HasIdentifier(this CommitAttempt attempt)
		{
			return attempt.CommitId != Guid.Empty;
		}

		public static bool IsPositive(this long value)
		{
			return value > 0;
		}

		public static bool HasEvents(this CommitAttempt attempt)
		{
			return attempt != null && attempt.Events.Count > 0;
		}
	}
}