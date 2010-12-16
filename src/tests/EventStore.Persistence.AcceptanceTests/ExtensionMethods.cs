namespace EventStore.Persistence.AcceptanceTests
{
	using System;

	internal static class ExtensionMethods
	{
		public static CommitAttempt BuildAttempt(this Guid streamId)
		{
			return streamId.BuildAttempt(Guid.NewGuid());
		}
		public static CommitAttempt BuildAttempt(this Guid streamId, Guid commitId)
		{
			return new CommitAttempt
			{
				StreamId = streamId,
				StreamName = "AcceptanceTestAttempt",
				CommitId = commitId,
				PreviousCommitSequence = 0,
				PreviousStreamRevision = 0,
				Events =
				{
					new EventMessage(),
					new EventMessage()
				}
			};
		}
	}
}