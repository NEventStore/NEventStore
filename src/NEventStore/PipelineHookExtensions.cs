namespace NEventStore
{
    /// <summary>
    ///    Provides extension methods for <see cref="IPipelineHook"/>.
    /// </summary>
    public static class PipelineHookExtensions
    {
        /// <summary>
        ///     Invoked when all buckets have been purged.
        /// </summary>
        /// <param name="pipelineHook">The pipeline hook.</param>
        public static void OnPurge(this IPipelineHook pipelineHook)
        {
            pipelineHook.OnPurge(null);
        }

        /// <summary>
        ///     Invoked when all buckets have been purged.
        /// </summary>
        /// <param name="pipelineHook">The pipeline hook.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static Task OnPurgeAsync(this IPipelineHookAsync pipelineHook, CancellationToken cancellationToken)
        {
            return pipelineHook.OnPurgeAsync(null, cancellationToken);
        }
    }
}