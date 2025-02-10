
namespace NEventStore
{
    /// <summary>
    ///    Provides a base implementation of the <see cref="IPipelineHookAsync"/> interface.
    /// </summary>
    public abstract class PipelineHookAsyncBase : IPipelineHookAsync
    {
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

        /// <inheritdoc/>
        public virtual Task OnDeleteStreamAsync(string bucketId, string streamId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task OnPurgeAsync(string? bucketId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task PostCommitAsync(ICommit committed, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task<bool> PreCommitAsync(CommitAttempt attempt, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public virtual Task<ICommit?> SelectCommitAsync(ICommit committed, CancellationToken cancellationToken)
        {
            return Task.FromResult<ICommit?>(committed);
        }
    }
}