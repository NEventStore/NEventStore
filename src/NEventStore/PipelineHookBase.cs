namespace NEventStore
{
    public abstract class PipelineHookBase : IPipelineHook
    {
        public virtual void Dispose()
        {}

        public virtual ICommit Select(ICommit committed)
        {
            return committed;
        }

        public virtual bool PreCommit(CommitAttempt attempt)
        {
            return true;
        }

        public virtual void PostCommit(ICommit committed)
        {}

        public virtual void OnPurge(string bucketId)
        {}

        public virtual void OnDeleteStream(string bucketId, string streamId)
        {}
    }
}