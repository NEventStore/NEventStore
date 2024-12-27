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
        public IEnumerable<ICommit> GetFrom(string bucketId, DateTime startDate) {
            return ExecuteHooks(_original.GetFrom(bucketId, startDate));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime startDate, DateTime endDate) {
            return ExecuteHooks(_original.GetFromTo(bucketId, startDate, endDate));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(Int64 checkpointToken) {
            return ExecuteHooks(_original.GetFrom(checkpointToken));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(Int64 fromCheckpointToken, Int64 toCheckpointToken) {
            return ExecuteHooks(_original.GetFromTo(fromCheckpointToken, toCheckpointToken));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFrom(string bucketId, Int64 checkpointToken) {
            return ExecuteHooks(_original.GetFrom(bucketId, checkpointToken));
        }

        /// <inheritdoc/>
        public IEnumerable<ICommit> GetFromTo(string bucketId, Int64 fromCheckpointToken, Int64 toCheckpointToken) {
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
                ICommit filtered = commit;
                foreach (var hook in _pipelineHooks.Where(x => (filtered = x.Select(filtered)) == null))
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
    }
}