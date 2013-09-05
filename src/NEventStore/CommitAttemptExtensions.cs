namespace NEventStore
{
    using NEventStore.Persistence;

    internal static class CommitAttemptExtensions
    {
        public static ICommit ToCommit(this CommitAttempt attempt, string checkpointToken)
        {
            return new Commit(
                attempt.BucketId,
                attempt.StreamId,
                attempt.StreamRevision,
                attempt.CommitId,
                attempt.CommitSequence,
                attempt.CommitStamp,
                checkpointToken,
                attempt.Headers,
                attempt.Events);
        }
    }
}