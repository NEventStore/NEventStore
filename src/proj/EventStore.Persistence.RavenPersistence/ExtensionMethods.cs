namespace EventStore.Persistence.RavenPersistence
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Raven.Database.Data;
	using Raven.Database.Json;

	internal static class ExtensionMethods
	{
		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}

		public static PatchCommandData RemoveUndispatchedProperty(this RavenCommit commit)
		{
			return new PatchCommandData
			{
				Key = commit.Id,
				Patches = new[]
				{
					new PatchRequest
					{
						Type = PatchCommandType.Remove,
						Name = "Dispatch"
					}
				}
			};
		}

		public static RavenCommit ToRavenCommit(this CommitAttempt attempt)
		{
			return attempt.ToCommit().ToRavenCommit();
		}
		public static RavenCommit ToRavenCommit(this Commit commit)
		{
			return new RavenCommit
			{
				StreamId = commit.StreamId,
				CommitId = commit.CommitId,
				StreamRevision = commit.StreamRevision,
				CommitSequence = commit.CommitSequence,
				Headers = (Dictionary<string, object>)commit.Headers,
				Events = commit.Events.ToList(),
				Snapshot = commit.Snapshot
			};
		}
		public static Commit ToCommit(this RavenCommit commit)
		{
			return new Commit(
				commit.StreamId,
				commit.CommitId,
				commit.StreamRevision,
				commit.CommitSequence,
				commit.Headers,
				commit.Events,
				commit.Snapshot);
		}
	}
}