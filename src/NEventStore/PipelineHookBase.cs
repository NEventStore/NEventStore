
namespace NEventStore
{
    /// <summary>
    ///    Provides a base implementation of the <see cref="IPipelineHook"/> interface.
    /// </summary>
    public abstract class PipelineHookBase : IPipelineHook
    {
        /// <inheritdoc/>
        public virtual ICommit? SelectCommit(ICommit committed)
        {
            return committed;
        }

        /// <inheritdoc/>
        public virtual bool PreCommit(CommitAttempt attempt)
        {
            return true;
        }

        /// <inheritdoc/>
        public virtual void PostCommit(ICommit committed)
        { }

        /// <inheritdoc/>
        public virtual void OnPurge(string? bucketId)
        { }

        /// <inheritdoc/>
        public virtual void OnDeleteStream(string bucketId, string streamId)
        { }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            // Cleanup
        }
    }
}