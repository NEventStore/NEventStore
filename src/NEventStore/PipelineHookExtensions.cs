namespace NEventStore;

public static class PipelineHookExtensions
{
    /// <summary>
    ///     Invoked when all buckets have been purged.
    /// </summary>
    /// <param name="pipelineHook">The pipleine hook.</param>
    public static void OnPurge(this IPipelineHook pipelineHook)
    {
        pipelineHook.OnPurge(null);
    }
}