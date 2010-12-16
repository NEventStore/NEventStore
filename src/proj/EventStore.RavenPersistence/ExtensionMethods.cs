namespace EventStore.RavenPersistence
{
	using System;
	using System.Globalization;

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

		public static RavenCommit ToRavenCommit(this CommitAttempt attempt)
		{
			var commit = attempt.ToCommit();
			return new RavenCommit
			{
				StreamId = commit.StreamId,
				CommitId = commit.CommitId,
				StreamRevision = commit.StreamRevision,
				CommitSequence = commit.CommitSequence,
				Headers = commit.Headers,
				Events = commit.Events
			};
		}
	}
}