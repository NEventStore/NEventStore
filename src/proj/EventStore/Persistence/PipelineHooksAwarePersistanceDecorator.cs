namespace EventStore.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    public class PipelineHooksAwarePersistanceDecorator : IPersistStreams
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (PipelineHooksAwarePersistanceDecorator));
        private readonly IPersistStreams original;
        private readonly IEnumerable<IPipelineHook> pipelineHooks;

        public PipelineHooksAwarePersistanceDecorator(IPersistStreams original, IEnumerable<IPipelineHook> pipelineHooks)
        {
            if (original == null) throw new ArgumentNullException("original");
            if (pipelineHooks == null) throw new ArgumentNullException("pipelineHooks");
            this.original = original;
            this.pipelineHooks = pipelineHooks;
        }

        public void Dispose()
        {
            original.Dispose();
        }
 
        public IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
        {
            return original.GetFrom(streamId, minRevision, maxRevision);
        }

        public void Commit(Commit attempt)
        {
            original.Commit(attempt);
        }

        public Snapshot GetSnapshot(Guid streamId, int maxRevision)
        {
            return original.GetSnapshot(streamId, maxRevision);
        }

        public bool AddSnapshot(Snapshot snapshot)
        {
            return original.AddSnapshot(snapshot);
        }

        public IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
        {
            return original.GetStreamsToSnapshot(maxThreshold);
        }

        public void Initialize()
        {
            original.Initialize();
        }

        public IEnumerable<Commit> GetFrom(DateTime start)
        {
            return ExecuteHooks(original.GetFrom(start));
        }

        public IEnumerable<Commit> GetFromTo(DateTime start, DateTime end)
        {
            return ExecuteHooks(original.GetFromTo(start, end));
        }

        public IEnumerable<Commit> GetUndispatchedCommits()
        {
            return original.GetUndispatchedCommits();
        }

        public void MarkCommitAsDispatched(Commit commit)
        {
            original.MarkCommitAsDispatched(commit);
        }

        public void Purge()
        {
            original.Purge();
        }

        public bool IsDisposed
        {
            get { return original.IsDisposed; }
        }

        private IEnumerable<Commit> ExecuteHooks(IEnumerable<Commit> commits)
        {
            foreach (Commit commit in commits)
            {
                Commit filtered = commit;
                foreach (IPipelineHook hook in pipelineHooks.Where(x => (filtered = x.Select(filtered)) == null))
                {
                    Logger.Info(Resources.PipelineHookSkippedCommit, hook.GetType(), commit.CommitId);
                    break;
                }

                if (filtered == null)
                    Logger.Info(Resources.PipelineHookFilteredCommit);
                else
                    yield return filtered;
            }
        }
    }
}