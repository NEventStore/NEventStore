using Microsoft.Extensions.Logging;
using NEventStore.Logging;
using NEventStore.Persistence;

namespace NEventStore
{
    /// <summary>
    ///    An implementation of a store that supports optimistic concurrency.
    /// </summary>
    public class OptimisticEventStore : IStoreEvents, ICommitEvents
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(OptimisticEventStore));
        private readonly IPersistStreams _persistence;
        private readonly IEnumerable<IPipelineHook> _pipelineHooks;

        /// <inheritdoc/>
        public virtual IPersistStreams Advanced { get => _persistence; }

        /// <summary>
        /// Initializes a new instance of the OptimisticEventStore class.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public OptimisticEventStore(IPersistStreams persistence, IEnumerable<IPipelineHook>? pipelineHooks)
        {
            if (persistence == null)
            {
                throw new ArgumentNullException(nameof(persistence));
            }

            _pipelineHooks = pipelineHooks ?? [];
            if (_pipelineHooks.Any())
            {
                _persistence = new PipelineHooksAwarePersistStreamsDecorator(persistence, _pipelineHooks);
            }
            else
            {
                _persistence = persistence;
            }
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return _persistence.GetFrom(bucketId, streamId, minRevision, maxRevision);
        }

        /// <inheritdoc/>
        public virtual ICommit? Commit(CommitAttempt attempt)
        {
            Guard.NotNull(() => attempt, attempt);
            foreach (var hook in _pipelineHooks)
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace(Resources.InvokingPreCommitHooks, attempt.CommitId, hook.GetType());
                }
                if (hook.PreCommit(attempt))
                {
                    continue;
                }

                if (Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation(Resources.CommitRejectedByPipelineHook, hook.GetType(), attempt.CommitId);
                }
                return null;
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Resources.CommittingAttempt, attempt.CommitId, attempt.Events?.Count ?? 0);
            }
            var commit = _persistence.Commit(attempt);

            if (commit != null)
            {
                foreach (var hook in _pipelineHooks)
                {
                    if (Logger.IsEnabled(LogLevel.Trace))
                    {
                        Logger.LogTrace(Resources.InvokingPostCommitPipelineHooks, attempt.CommitId, hook.GetType());
                    }
                    hook.PostCommit(commit);
                }
            }
            return commit;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public virtual IEventStream CreateStream(string bucketId, string streamId)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Resources.CreatingStream, streamId, bucketId);
            }
            return new OptimisticEventStream(bucketId, streamId, this);
        }

        /// <inheritdoc/>
        public virtual IEventStream OpenStream(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Resources.OpeningStreamAtRevision, streamId, bucketId, minRevision, maxRevision);
            }
            var stream = new OptimisticEventStream(bucketId, streamId, this);
            stream.Initialize(minRevision, maxRevision);
            return stream;
        }

        /// <inheritdoc/>
        public virtual IEventStream OpenStream(ISnapshot snapshot, int maxRevision)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Resources.OpeningStreamWithSnapshot, snapshot.StreamId, snapshot.BucketId, snapshot.StreamRevision, maxRevision);
            }
            maxRevision = maxRevision <= 0 ? int.MaxValue : maxRevision;
            var stream = new OptimisticEventStream(snapshot.BucketId, snapshot.StreamId, this);
            stream.Initialize(snapshot, maxRevision);
            return stream;
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Resources.ShuttingDownStore);
            }
            _persistence.Dispose();
            foreach (var hook in _pipelineHooks)
            {
                hook.Dispose();
            }
        }
    }
}