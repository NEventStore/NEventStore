namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    public class OptimisticEventStore : IStoreEvents, ICommitEvents
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(OptimisticEventStore));
        private readonly IPersistStreams _persistence;
        private readonly IEnumerable<IPipelineHook> _pipelineHooks;

        public virtual IPersistStreams Advanced { get => _persistence; }

        public OptimisticEventStore(IPersistStreams persistence, IEnumerable<IPipelineHook> pipelineHooks)
        {
            if (persistence == null)
            {
                throw new ArgumentNullException(nameof(persistence));
            }

            _pipelineHooks = pipelineHooks ?? new IPipelineHook[0];
            if (_pipelineHooks.Any())
            {
                _persistence = new PipelineHooksAwarePersistanceDecorator(persistence, _pipelineHooks);
            }
            else
            {
                _persistence = persistence;
            }
        }

        public virtual IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return _persistence.GetFrom(bucketId, streamId, minRevision, maxRevision);
        }

        public virtual ICommit Commit(CommitAttempt attempt)
        {
            Guard.NotNull(() => attempt, attempt);
            foreach (var hook in _pipelineHooks)
            {
                Logger.LogTrace(Resources.InvokingPreCommitHooks, attempt.CommitId, hook.GetType());
                if (hook.PreCommit(attempt))
                {
                    continue;
                }

                Logger.LogInformation(Resources.CommitRejectedByPipelineHook, hook.GetType(), attempt.CommitId);
                return null;
            }

            Logger.LogTrace(Resources.CommittingAttempt, attempt.CommitId, attempt.Events?.Length ?? 0);
            ICommit commit = _persistence.Commit(attempt);

            foreach (var hook in _pipelineHooks)
            {
                Logger.LogTrace(Resources.InvokingPostCommitPipelineHooks, attempt.CommitId, hook.GetType());
                hook.PostCommit(commit);
            }
            return commit;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual IEventStream CreateStream(string bucketId, string streamId)
        {
            Logger.LogDebug(Resources.CreatingStream, streamId, bucketId);
            return new OptimisticEventStream(bucketId, streamId, this);
        }

        public virtual IEventStream OpenStream(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;

            Logger.LogTrace(Resources.OpeningStreamAtRevision, streamId, bucketId, minRevision, maxRevision);
            return new OptimisticEventStream(bucketId, streamId, this, minRevision, maxRevision);
        }

        public virtual IEventStream OpenStream(ISnapshot snapshot, int maxRevision)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            Logger.LogTrace(Resources.OpeningStreamWithSnapshot, snapshot.StreamId, snapshot.BucketId, snapshot.StreamRevision, maxRevision);
            maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
            return new OptimisticEventStream(snapshot, this, maxRevision);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Logger.LogInformation(Resources.ShuttingDownStore);
            _persistence.Dispose();
            foreach (var hook in _pipelineHooks)
            {
                hook.Dispose();
            }
        }
    }
}