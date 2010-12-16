namespace EventStore.RavenPersistence
{
	internal static class ExtensionMethods
	{
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