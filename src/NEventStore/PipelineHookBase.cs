using System;

namespace NEventStore;

public abstract class PipelineHookBase : IPipelineHook
{
    public virtual ICommit Select(ICommit committed)
    {
        return committed;
    }

    public virtual bool PreCommit(CommitAttempt attempt)
    {
        return true;
    }

    public virtual void PostCommit(ICommit committed)
    {
    }

    public virtual void OnPurge(string bucketId)
    {
    }

    public virtual void OnDeleteStream(string bucketId, string streamId)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Cleanup
    }
}