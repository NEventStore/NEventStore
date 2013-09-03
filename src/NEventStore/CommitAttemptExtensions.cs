namespace NEventStore
{
    using NEventStore.Persistence;

    internal static class CommitAttemptExtensions
    {
        public static ICommit ToCommit(this CommitAttempt attempt, ICheckpoint checkpoint)
        {
            return new Commit(
                attempt.BucketId,
                attempt.StreamId,
                attempt.StreamRevision,
                attempt.CommitId,
                attempt.CommitSequence,
                attempt.CommitStamp,
                checkpoint,
                attempt.Headers,
                attempt.Events);
        }
    }
}