namespace NEventStore
{
    using System;
    using NEventStore.Persistence;

    internal static class CommitHelper
    {
        public static ICommit Create()
        {
            return new Commit(Bucket.Default, "defaultstream", 0, Guid.NewGuid(), 0, DateTime.MinValue, new IntCheckpoint(0).Value, null, null);
        }
    }
}