using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore.Persistence
{
    /// <summary>
    ///    Represents a persistence decorator that allows for hooks to be injected into the pipeline.
    /// </summary>
    public sealed class PipelineHooksAwarePersistStreamsDecorator : IPersistStreams
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(PipelineHooksAwarePersistStreamsDecorator));
        private readonly IPersistStreams _original;
        private readonly IEnumerable<IPipelineHook> _pipelineHooks;

        /// <summary>
        /// Initializes a new instance of the PipelineHooksAwarePersistStreamsDecorator class.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public PipelineHooksAwarePersistStreamsDecorator(IPersistStreams original, IEnumerable<IPipelineHook> pipelineHooks)
        {
            _original = original ?? throw new ArgumentNullException(nameof(original));
            _pipelineHooks = pipelineHooks ?? throw new ArgumentNullException(nameof(pipelineHooks));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _original.Dispose();
        }

        /// <inheritdoc/>
        [Obsolete("DateTime is problematic in distributed systems. Use GetFrom(Int64 checkpointToken) instead. This method will be removed in a later version.")]
        public IEnumerable<ICommit> GetFrom(string bucketId, DateTime startDate)
        {
            return ExecuteHooks(_original.GetFrom(bucketId, startDate));
        }

        /// <inheritdoc/>
        [Obsolete("DateTime is problematic in distributed systems. Use GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken) instead. This method will be removed in a later version.")]
        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime startDate, DateTime endDate)
        {
            return ExecuteHooks(_original.GetFromTo(bucketId, startDate, endDate));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(Int64 checkpointToken)
        {
            return ExecuteHooks(_original.GetFrom(checkpointToken));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken)
        {
            return ExecuteHooks(_original.GetFromTo(fromCheckpointToken, toCheckpointToken));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, Int64 checkpointToken)
        {
            return ExecuteHooks(_original.GetFrom(bucketId, checkpointToken));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(string bucketId, Int64 fromCheckpointToken, Int64 toCheckpointToken)
        {
            return ExecuteHooks(_original.GetFromTo(bucketId, fromCheckpointToken, toCheckpointToken));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return ExecuteHooks(_original.GetFrom(bucketId, streamId, minRevision, maxRevision));
        }

        /// <inheritdoc/>
        public ICommit? Commit(CommitAttempt attempt)
        {
            return _original.Commit(attempt);
        }

        /// <inheritdoc/>
        public Task GetFromAsync(string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> observer, CancellationToken cancellationToken)
        {
            var pipelineHookObserver = new PipelineHookObserver(_pipelineHooks, observer);
            return _original.GetFromAsync(bucketId, streamId, minRevision, maxRevision, pipelineHookObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<ICommit?> CommitAsync(CommitAttempt attempt, CancellationToken cancellationToken)
        {
            return _original.CommitAsync(attempt, cancellationToken);
        }

        /// <inheritdoc/>
        public ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
        {
            return _original.GetSnapshot(bucketId, streamId, maxRevision);
        }

        /// <inheritdoc/>
        public bool AddSnapshot(ISnapshot snapshot)
        {
            return _original.AddSnapshot(snapshot);
        }

        /// <inheritdoc/>
        public IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
        {
            return _original.GetStreamsToSnapshot(bucketId, maxThreshold);
        }

        /// <inheritdoc/>
        public Task<ISnapshot> GetSnapshotAsync(string bucketId, string streamId, int maxRevision, CancellationToken cancellationToken)
        {
            return _original.GetSnapshotAsync(bucketId, streamId, maxRevision, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<bool> AddSnapshotAsync(ISnapshot snapshot, CancellationToken cancellationToken)
        {
            return _original.AddSnapshotAsync(snapshot, cancellationToken);
        }

        /// <inheritdoc/>
        public Task GetStreamsToSnapshotAsync(string bucketId, int maxThreshold, IAsyncObserver<IStreamHead> asyncObserver, CancellationToken cancellationToken)
        {
            return _original.GetStreamsToSnapshotAsync(bucketId, maxThreshold, asyncObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            _original.Initialize();
        }

        /// <inheritdoc/>
        public void Purge()
        {
            _original.Purge();
            foreach (var pipelineHook in _pipelineHooks)
            {
                pipelineHook.OnPurge();
            }
        }

        /// <inheritdoc/>
        public void Purge(string bucketId)
        {
            _original.Purge(bucketId);
            foreach (var pipelineHook in _pipelineHooks)
            {
                pipelineHook.OnPurge(bucketId);
            }
        }

        /// <inheritdoc/>
        public void Drop()
        {
            _original.Drop();
        }

        /// <inheritdoc/>
        public void DeleteStream(string bucketId, string streamId)
        {
            _original.DeleteStream(bucketId, streamId);
            foreach (var pipelineHook in _pipelineHooks)
            {
                pipelineHook.OnDeleteStream(bucketId, streamId);
            }
        }

        /// <inheritdoc/>
        public bool IsDisposed
        {
            get { return _original.IsDisposed; }
        }

        private IEnumerable<ICommit> ExecuteHooks(IEnumerable<ICommit> commits)
        {
            foreach (var commit in commits)
            {
                ICommit? filtered = commit;
                foreach (var hook in _pipelineHooks.Where(x => (filtered = x.SelectCommit(filtered)) == null))
                {
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation(Resources.PipelineHookSkippedCommit, hook.GetType(), commit.CommitId);
                    }
                    break;
                }

                if (filtered == null)
                {
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation(Resources.PipelineHookFilteredCommit);
                    }
                }
                else
                {
                    yield return filtered;
                }
            }
        }

        internal class PipelineHookObserver : IAsyncObserver<ICommit>
        {
            private readonly IEnumerable<IPipelineHook> _pipelineHooks;
            private readonly IAsyncObserver<ICommit> _observer;

            public PipelineHookObserver(
                IEnumerable<IPipelineHook> pipelineHooks,
                IAsyncObserver<ICommit> observer
                )
            {
                _pipelineHooks = pipelineHooks;
                _observer = observer;
            }

            public Task OnCompletedAsync(Int64 checkpoint)
            {
                return _observer.OnCompletedAsync(checkpoint);
            }

            public Task OnErrorAsync(Int64 checkpoint, Exception error)
            {
                return _observer.OnErrorAsync(checkpoint, error);
            }

            public Task OnNextAsync(ICommit value)
            {
                var commit = ExecuteHooks(value);
                if (commit != null)
                {
                    return _observer.OnNextAsync(commit);
                }
                return Task.CompletedTask;
            }

            private ICommit? ExecuteHooks(ICommit commit)
            {
                ICommit? filtered = commit;
                // todo: should Pipeline hooks be async?
                foreach (var hook in _pipelineHooks.Where(x => (filtered = x.SelectCommit(filtered)) == null))
                {
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation(Resources.PipelineHookSkippedCommit, hook.GetType(), commit.CommitId);
                    }
                    break;
                }

                if (filtered == null)
                {
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation(Resources.PipelineHookFilteredCommit);
                    }
                    return null;
                }
                else
                {
                    return filtered;
                }
            }
        }
    }
}