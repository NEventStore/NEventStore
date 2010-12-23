namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using Persistence;

	internal static class ExtensionMethods
	{
		public static void AddEventsOrClearOnSnapshot(
			this ICollection<object> events, Commit commit, bool applySnapshot)
		{
			if (applySnapshot && commit.Snapshot != null)
				events.Clear();
			else
				events.AddEvents(commit);
		}

		public static void AddEvents(this ICollection<object> @events, Commit commit)
		{
			foreach (var @event in commit.Events)
				events.Add(@event.Body);
		}

		public static bool IsValid(this CommitAttempt attempt)
		{
			if (attempt == null)
				throw new ArgumentNullException("attempt");

			if (!attempt.HasIdentifier())
				throw new ArgumentException("The commit must be uniquely identified.", "attempt");

			if (attempt.PreviousCommitSequence < 0)
				throw new ArgumentException("The commit sequence cannot be a negative number.", "attempt");

			if (attempt.StreamRevision <= 0)
				throw new ArgumentException("The stream revision must be a positive number.", "attempt");

			if (attempt.StreamRevision <= attempt.PreviousCommitSequence)
				throw new ArgumentException("The stream revision must always be greater than the previous commit sequence.", "attempt");

			return true;
		}

		public static bool HasIdentifier(this CommitAttempt attempt)
		{
			return attempt.StreamId != Guid.Empty && attempt.CommitId != Guid.Empty;
		}

		public static bool IsEmpty(this CommitAttempt attempt)
		{
			return attempt == null || attempt.Events.Count == 0;
		}
	}
}