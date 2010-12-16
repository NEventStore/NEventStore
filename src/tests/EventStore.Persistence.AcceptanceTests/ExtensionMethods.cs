namespace EventStore.Persistence.AcceptanceTests
{
	using System;

	internal static class ExtensionMethods
	{
		public static CommitAttempt BuildAttempt(this Guid streamId)
		{
			return new CommitAttempt
			{
				StreamId = streamId,
				StreamName = "AcceptanceTestAttempt",
				CommitId = Guid.NewGuid(),
				PreviousCommitSequence = 0,
				PreviousStreamRevision = 0,
				Events =
				{
					new EventMessage(),
					new EventMessage()
				}
			};
		}
		public static CommitAttempt BuildNextAttempt(this CommitAttempt successful)
		{
			var commit = successful.ToCommit();
			return new CommitAttempt
			{
				StreamId = commit.StreamId,
				CommitId = Guid.NewGuid(),
				PreviousCommitSequence = commit.CommitSequence,
				PreviousStreamRevision = commit.StreamRevision,
				Events =
				{
					new EventMessage(),
					new EventMessage()
				}
			};
		}
	}
}