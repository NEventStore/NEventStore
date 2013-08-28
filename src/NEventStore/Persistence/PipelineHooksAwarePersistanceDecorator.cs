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

        public IEnumerable<Commit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return _original.GetFrom(bucketId, streamId, minRevision, maxRevision);
        }

        public void Commit(Commit attempt)
        {
            _original.Commit(attempt);
        }

        public Snapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            return _original.GetSnapshot(bucketId, streamId, maxRevision);
        }

        public bool AddSnapshot(Snapshot snapshot)
        {
            return _original.AddSnapshot(snapshot);
        }

        public IEnumerable<StreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            return _original.GetStreamsToSnapshot(bucketId, maxThreshold);
        }

        public void Initialize()
        {
            _original.Initialize();
        }

        public IEnumerable<Commit> GetFrom(string bucketId, DateTime start)
        {
            return ExecuteHooks(_original.GetFrom(bucketId, start));
        }

        public IEnumerable<Commit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            return ExecuteHooks(_original.GetFromTo(bucketId, start, end));
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

        public void Purge(string bucketId)
        {
            _original.Purge(bucketId);
        }

        public void Drop()
        {
            _original.Drop();
        }

        public IEnumerable<Commit> GetSince(int checkpoint)
        {
            return _original.GetSince(checkpoint);
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