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

        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return _original.GetFrom(bucketId, streamId, minRevision, maxRevision);
        }

        public ICommit Commit(CommitAttempt attempt)
        {
            return _original.Commit(attempt);
        }

        public ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            return _original.GetSnapshot(bucketId, streamId, maxRevision);
        }

        public bool AddSnapshot(ISnapshot snapshot)
        {
            return _original.AddSnapshot(snapshot);
        }

        public IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            return _original.GetStreamsToSnapshot(bucketId, maxThreshold);
        }

        public void Initialize()
        {
            _original.Initialize();
        }

        public IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
        {
            return ExecuteHooks(_original.GetFrom(bucketId, start));
        }

        public IEnumerable<ICommit> GetFromBeginning()
        {
            return _original.GetFromBeginning();
        }

        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime start, DateTime end)
        {
            return ExecuteHooks(_original.GetFromTo(bucketId, start, end));
        }

        public IEnumerable<ICommit> GetUndispatchedCommits()
        {
            return _original.GetUndispatchedCommits();
        }

        public void MarkCommitAsDispatched(ICommit commit)
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

        public void DeleteStream(string bucketId, string streamId)
        {
            _original.DeleteStream(bucketId, streamId);
        }

        public IEnumerable<ICommit> GetFrom(ICheckpoint checkpoint)
        {
            return _original.GetFrom(checkpoint);
        }

        public bool IsDisposed
        {
            get { return _original.IsDisposed; }
        }

        private IEnumerable<ICommit> ExecuteHooks(IEnumerable<ICommit> commits)
        {
            foreach (var commit in commits)
            {
                ICommit filtered = commit;
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