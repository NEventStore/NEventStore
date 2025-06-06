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
        private readonly IEnumerable<IPipelineHookAsync> _pipelineHooksAsync;

        /// <summary>
        /// Initializes a new instance of the PipelineHooksAwarePersistStreamsDecorator class.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public PipelineHooksAwarePersistStreamsDecorator(IPersistStreams original, IEnumerable<IPipelineHook> pipelineHooks, IEnumerable<IPipelineHookAsync> pipelineHooksAsync)
        {
            _original = original ?? throw new ArgumentNullException(nameof(original));
            _pipelineHooks = pipelineHooks ?? throw new ArgumentNullException(nameof(pipelineHooks));
            _pipelineHooksAsync = pipelineHooksAsync ?? throw new ArgumentNullException(nameof(pipelineHooksAsync));
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
            return ExecuteSelectCommitsHooks(_original.GetFrom(bucketId, startDate));
        }

        /// <inheritdoc/>
        [Obsolete("DateTime is problematic in distributed systems. Use GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken) instead. This method will be removed in a later version.")]
        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime startDate, DateTime endDate)
        {
            return ExecuteSelectCommitsHooks(_original.GetFromTo(bucketId, startDate, endDate));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(Int64 checkpointToken)
        {
            return ExecuteSelectCommitsHooks(_original.GetFrom(checkpointToken));
        }

        /// <inheritdoc/>
        public Task GetFromAsync(Int64 checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            var pipelineHookObserver = new PipelineHookObserver(_pipelineHooks, _pipelineHooksAsync, asyncObserver);
            return _original.GetFromAsync(checkpointToken, pipelineHookObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken)
        {
            return ExecuteSelectCommitsHooks(_original.GetFromTo(fromCheckpointToken, toCheckpointToken));
        }

        /// <inheritdoc/>
        public Task GetFromToAsync(Int64 fromCheckpointToken, Int64 toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            var pipelineHookObserver = new PipelineHookObserver(_pipelineHooks, _pipelineHooksAsync, asyncObserver);
            return _original.GetFromToAsync(fromCheckpointToken, toCheckpointToken, pipelineHookObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, Int64 checkpointToken)
        {
            return ExecuteSelectCommitsHooks(_original.GetFrom(bucketId, checkpointToken));
        }

        /// <inheritdoc/>
        public Task GetFromAsync(string bucketId, Int64 checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            var pipelineHookObserver = new PipelineHookObserver(_pipelineHooks, _pipelineHooksAsync, asyncObserver);
            return _original.GetFromAsync(bucketId, checkpointToken, pipelineHookObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(string bucketId, Int64 fromCheckpointToken, Int64 toCheckpointToken)
        {
            return ExecuteSelectCommitsHooks(_original.GetFromTo(bucketId, fromCheckpointToken, toCheckpointToken));
        }

        /// <inheritdoc/>
        public Task GetFromToAsync(string bucketId, long fromCheckpointToken, long toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellationToken)
        {
            var pipelineHookObserver = new PipelineHookObserver(_pipelineHooks, _pipelineHooksAsync, asyncObserver);
            return _original.GetFromToAsync(bucketId, fromCheckpointToken, toCheckpointToken, pipelineHookObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return ExecuteSelectCommitsHooks(_original.GetFrom(bucketId, streamId, minRevision, maxRevision));
        }

        /// <inheritdoc/>
        public ICommit? Commit(CommitAttempt attempt)
        {
            return _original.Commit(attempt);
        }

        /// <inheritdoc/>
        public Task GetFromAsync(string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> observer, CancellationToken cancellationToken)
        {
            var pipelineHookObserver = new PipelineHookObserver(_pipelineHooks, _pipelineHooksAsync, observer);
            return _original.GetFromAsync(bucketId, streamId, minRevision, maxRevision, pipelineHookObserver, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<ICommit?> CommitAsync(CommitAttempt attempt, CancellationToken cancellationToken)
        {
            return _original.CommitAsync(attempt, cancellationToken);
        }

        /// <inheritdoc/>
        public ISnapshot? GetSnapshot(string bucketId, string streamId, int maxRevision)
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
        public Task<ISnapshot?> GetSnapshotAsync(string bucketId, string streamId, int maxRevision, CancellationToken cancellationToken)
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
            foreach (var pipelineHook in _pipelineHooksAsync)
            {
                pipelineHook.OnPurgeAsync(CancellationToken.None).GetAwaiter().GetResult();
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
            foreach (var pipelineHook in _pipelineHooksAsync)
            {
                pipelineHook.OnPurgeAsync(bucketId, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        /// <inheritdoc/>
        public async Task PurgeAsync(CancellationToken cancellationToken)
        {
            await _original.PurgeAsync(cancellationToken).ConfigureAwait(false);
            foreach (var pipelineHook in _pipelineHooks)
            {
                pipelineHook.OnPurge();
            }
            foreach (var pipelineHook in _pipelineHooksAsync)
            {
                await pipelineHook.OnPurgeAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task PurgeAsync(string bucketId, CancellationToken cancellationToken)
        {
            await _original.PurgeAsync(bucketId, cancellationToken).ConfigureAwait(false);
            foreach (var pipelineHook in _pipelineHooks)
            {
                pipelineHook.OnPurge(bucketId);
            }
            foreach (var pipelineHook in _pipelineHooksAsync)
            {
                await pipelineHook.OnPurgeAsync(bucketId, cancellationToken).ConfigureAwait(false);
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
            foreach (var pipelineHook in _pipelineHooksAsync)
            {
                pipelineHook.OnDeleteStreamAsync(bucketId, streamId, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        /// <inheritdoc/>
        public async Task DeleteStreamAsync(string bucketId, string streamId, CancellationToken cancellationToken)
        {
            await _original.DeleteStreamAsync(bucketId, streamId, cancellationToken).ConfigureAwait(false);
            foreach (var pipelineHook in _pipelineHooks)
            {
                pipelineHook.OnDeleteStream(bucketId, streamId);
            }
            foreach (var pipelineHook in _pipelineHooksAsync)
            {
                await pipelineHook.OnDeleteStreamAsync(bucketId, streamId, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public bool IsDisposed
        {
            get { return _original.IsDisposed; }
        }

        private IEnumerable<ICommit> ExecuteSelectCommitsHooks(IEnumerable<ICommit> commits)
        {
            foreach (var commit in commits)
            {
                ICommit? filtered = commit;
                foreach (var hook in _pipelineHooks)
                {
                    filtered = hook.SelectCommit(filtered);
                    if (filtered == null)
                    {
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation(Resources.PipelineHookSkippedCommit, hook.GetType(), commit.CommitId);
                        }
                        break;
                    }
                }
                if (filtered != null)
                {
                    foreach (var hook in _pipelineHooksAsync)
                    {
                        filtered = hook.SelectCommitAsync(filtered, CancellationToken.None).GetAwaiter().GetResult();
                        if (filtered == null)
                        {
                            if (Logger.IsEnabled(LogLevel.Information))
                            {
                                Logger.LogInformation(Resources.PipelineHookSkippedCommit, hook.GetType(), commit.CommitId);
                            }
                            break;
                        }
                    }
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
            private readonly IEnumerable<IPipelineHookAsync> _pipelineHooksAsync;
            private readonly IAsyncObserver<ICommit> _observer;

            public PipelineHookObserver(
                IEnumerable<IPipelineHook> pipelineHooks,
                IEnumerable<IPipelineHookAsync> pipelineHooksAsync,
                IAsyncObserver<ICommit> observer
                )
            {
                _pipelineHooks = pipelineHooks;
                _pipelineHooksAsync = pipelineHooksAsync;
                _observer = observer;
            }

            public Task OnCompletedAsync(CancellationToken cancellationToken)
            {
                return _observer.OnCompletedAsync(cancellationToken);
            }

            public Task OnErrorAsync(Exception error, CancellationToken cancellationToken)
            {
                return _observer.OnErrorAsync(error, cancellationToken);
            }

            public async Task<bool> OnNextAsync(ICommit value, CancellationToken cancellationToken)
            {
                var commit = await ExecuteSelectCommitHooksAsync(value, cancellationToken).ConfigureAwait(false);
                if (commit != null)
                {
                    return await _observer.OnNextAsync(commit, cancellationToken).ConfigureAwait(false);
                }
                return true;
            }

            private async Task<ICommit?> ExecuteSelectCommitHooksAsync(ICommit commit, CancellationToken cancellationToken)
            {
                ICommit? filtered = commit;
                foreach (var hook in _pipelineHooks)
                {
                    filtered = hook.SelectCommit(filtered);
                    if (filtered == null)
                    {
                        if (Logger.IsEnabled(LogLevel.Information))
                        {
                            Logger.LogInformation(Resources.PipelineHookSkippedCommit, hook.GetType(), commit.CommitId);
                        }
                        break;
                    }
                }
                if (filtered != null)
                {
                    foreach (var hook in _pipelineHooksAsync)
                    {
                        filtered = await hook.SelectCommitAsync(filtered, cancellationToken).ConfigureAwait(false);
                        if (filtered == null)
                        {
                            if (Logger.IsEnabled(LogLevel.Information))
                            {
                                Logger.LogInformation(Resources.PipelineHookSkippedCommit, hook.GetType(), commit.CommitId);
                            }
                            break;
                        }
                    }
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