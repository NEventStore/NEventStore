namespace NEventStore.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Logging;

    public class PipelineHooksAwarePersistanceDecorator : IPersistStreams
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (PipelineHooksAwarePersistanceDecorator));
        private readonly IPersistStreams _original;
        private readonly IEnumerable<IPipelineHook> _pipelineHooks;

        public PipelineHooksAwarePersistanceDecorator(IPersistStreams original, IEnumerable<IPipelineHook> pipelineHooks)
        {
            if (original == null)
            {
                throw new ArgumentNullException("original");
            }
            if (pipelineHooks == null)
            {
                throw new ArgumentNullException("pipelineHooks");
            }
            _original = original;
            _pipelineHooks = pipelineHooks;
        }

        public void Dispose()
        {
            _original.Dispose();
        }

        public IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
        {
            return _original.GetFrom(streamId, minRevision, maxRevision);
        }

        public void Commit(Commit attempt)
        {
            _original.Commit(attempt);
        }

        public Snapshot GetSnapshot(Guid streamId, int maxRevision)
        {
            return _original.GetSnapshot(streamId, maxRevision);
        }

        public bool AddSnapshot(Snapshot snapshot)
        {
            return _original.AddSnapshot(snapshot);
        }

        public IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
        {
            return _original.GetStreamsToSnapshot(maxThreshold);
        }

        public void Initialize()
        {
            _original.Initialize();
        }

        public IEnumerable<Commit> GetFrom(DateTime start)
        {
            return ExecuteHooks(_original.GetFrom(start));
        }

        public IEnumerable<Commit> GetFromTo(DateTime start, DateTime end)
        {
            return ExecuteHooks(_original.GetFromTo(start, end));
        }

        public IEnumerable<Commit> GetUndispatchedCommits()
        {
            return _original.GetUndispatchedCommits();
        }

        public void MarkCommitAsDispatched(Commit commit)
        {
            _original.MarkCommitAsDispatched(commit);
        }

        public void Purge()
        {
            _original.Purge();
        }

        public bool IsDisposed
        {
            get { return _original.IsDisposed; }
        }

        private IEnumerable<Commit> ExecuteHooks(IEnumerable<Commit> commits)
        {
            foreach (var commit in commits)
            {
                Commit filtered = commit;
                foreach (var hook in _pipelineHooks.Where(x => (filtered = x.Select(filtered)) == null))
                {
                    Logger.Info(Resources.PipelineHookSkippedCommit, hook.GetType(), commit.CommitId);
                    break;
                }

                if (filtered == null)
                {
                    Logger.Info(Resources.PipelineHookFilteredCommit);
                }
                else
                {
                    yield return filtered;
                }
            }
        }
    }
}