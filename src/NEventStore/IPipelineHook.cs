using System;

namespace NEventStore
{
    /// <summary>
    ///     Provides the ability to hook into the pipeline of persisting a commit.
    /// </summary>
    /// <remarks>
    ///     Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    public interface IPipelineHook : IDisposable
    {
        /// <summary>
        ///     Hooks into the selection pipeline just prior to the commit being returned to the caller.
        /// </summary>
        /// <param name="committed">The commit to be filtered.</param>
        /// <returns>If successful, returns a populated commit; otherwise returns null.</returns>
        ICommit Select(ICommit committed);

        /// <summary>
        ///     Hooks into the commit pipeline prior to persisting the commit to durable storage.
        /// </summary>
        /// <param name="attempt">The attempt to be committed.</param>
        /// <returns>If processing should continue, returns true; otherwise returns false.</returns>
        bool PreCommit(CommitAttempt attempt);

        /// <summary>
        ///     Hooks into the commit pipeline just after the commit has been *successfully* committed to durable storage.
        /// </summary>
        /// <param name="committed">The commit which has been persisted.</param>
        void PostCommit(ICommit committed);

        /// <summary>
        ///     Invoked when a bucket has been purged. If buckedId is null, then all buckets have been purged.
        /// </summary>
        /// <param name="bucketId">The bucket Id that has been purged. Null when all buckets have been purged.</param>
        void OnPurge(string bucketId);

        /// <summary>
        ///     Invoked when a stream has been deleted.
        /// </summary>
        /// <param name="bucketId">The bucket Id from which the stream whch has been deleted.</param>
        /// <param name="streamId">The stream Id of the stream which has been deleted.</param>
        void OnDeleteStream(string bucketId, string streamId);
    }
}