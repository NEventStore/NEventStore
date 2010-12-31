namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json.Linq;
	using Raven.Database.Data;
	using Raven.Database.Json;

	internal static class RavenExtensions
	{
		public static PatchCommandData UpdateStream(this Guid streamId, string name, long headRevision)
		{
			var patches = new List<PatchRequest>
			{
				new PatchRequest
				{
					Type = PatchCommandType.Set,
					Name = "HeadRevision",
					Value = JToken.FromObject(headRevision)
				}
			};

			if (!string.IsNullOrEmpty(name))
				patches.Add(new PatchRequest
				{
					Type = PatchCommandType.Set,
					Name = "HeadRevision",
					Value = JToken.FromObject(headRevision)
				});

			return new PatchCommandData
			{
				Key = streamId.ToString(),
				Patches = patches.ToArray()
			};
		}
		public static PatchCommandData UpdateStream(this Guid streamId, long snapshotRevision)
		{
			return new PatchCommandData
			{
				Key = streamId.ToString(),
				Patches = new[]
				{
					new PatchRequest
					{
						Type = PatchCommandType.Set,
						Name = "SnapshotRevision",
						Value = JToken.FromObject(snapshotRevision)
					}
				}
			};
		}
		public static PatchCommandData RemoveProperty(this RavenCommit commit, string name)
		{
			return new PatchCommandData
			{
				Key = commit.Id,
				Patches = new[]
				{
					new PatchRequest
					{
						Type = PatchCommandType.Unset,
						Name = name,
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