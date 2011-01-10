namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using Raven.Database.Data;
	using Raven.Database.Json;

	internal static class RavenExtensions
	{
		private const string IdFormat = "{0}.{1}";

		public static string Id(this Commit commit)
		{
			return IdFormat.FormatWith(commit.StreamId, commit.CommitSequence);
		}

		public static PatchCommandData UpdateStream(this Guid streamId, string name, int headRevision)
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
		public static PatchCommandData UpdateStream(this Guid streamId, int snapshotRevision)
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
		public static PatchCommandData RemoveProperty(this Commit commit, string name)
		{
			return new PatchCommandData
			{
				Key = commit.Id(),
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
	}
}