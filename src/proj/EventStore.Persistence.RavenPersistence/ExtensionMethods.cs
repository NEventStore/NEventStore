namespace EventStore.Persistence.RavenPersistence
{
	using System.Globalization;
	using Persistence;
	using Raven.Database.Data;
	using Raven.Database.Json;

	internal static class ExtensionMethods
	{
		private const string IdFormat = "{0}.{1}";

		public static string Id(this Commit commit)
		{
			if (commit == null)
				return null;

			return IdFormat.FormatWith(commit.StreamId, commit.CommitSequence);
		}

		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}

		public static PatchCommandData RemoveUndispatchedProperty(this Commit commit)
		{
			return new PatchCommandData
			{
				Key = commit.Id(),
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
	}
}