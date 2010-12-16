namespace EventStore.RavenPersistence
{
	using System;
	using System.Globalization;
	using Raven.Database.Data;
	using Raven.Database.Json;

	internal static class ExtensionMethods
	{
		public static string ToHexString(this Guid value)
		{
			return value.ToString().Replace("-", string.Empty);
		}

		public static string FormatWith(this string format, params object[] values)
		{
			return string.Format(CultureInfo.InvariantCulture, format, values);
		}

		public static PatchCommandData RemoveUndispatchedProperty(this Commit commit)
		{
			return new PatchCommandData
			{
				Key = new RavenCommit(commit).Id,
				Patches = new[]
				{
					new PatchRequest
					{
						Type = PatchCommandType.Remove,
						Name = "PendingDispatch"
					}
				}
			};
		}
	}
}