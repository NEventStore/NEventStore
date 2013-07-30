namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using NEventStore.Logging;

    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "This behaves like a stream--not a .NET 'Stream' object, but a stream nonetheless.")]
    public class OptimisticEventStream : IEventStream
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (OptimisticEventStream));
        private readonly ICollection<EventMessage> _committed = new LinkedList<EventMessage>();
        private readonly IDictionary<string, object> _committedHeaders = new Dictionary<string, object>();
        private readonly ICollection<EventMessage> _events = new LinkedList<EventMessage>();
        private readonly ICollection<Guid> _identifiers = new HashSet<Guid>();
        private readonly ICommitEvents _persistence;
        private readonly IDictionary<string, object> _uncommittedHeaders = new Dictionary<string, object>();
        private bool _disposed;

        public OptimisticEventStream(Guid streamId, ICommitEvents persistence)
        {
            StreamId = streamId;
            _persistence = persistence;
        }

        public OptimisticEventStream(Guid streamId, ICommitEvents persistence, int minRevision, int maxRevision)
            : this(streamId, persistence)
        {
            IEnumerable<Commit> commits = persistence.GetFrom(streamId, minRevision, maxRevision);
            PopulateStream(minRevision, maxRevision, commits);

            if (minRevision > 0 && _committed.Count == 0)
            {
                throw new StreamNotFoundException();
            }
        }

        public OptimisticEventStream(Snapshot snapshot, ICommitEvents persistence, int maxRevision)
            : this(snapshot.StreamId, persistence)
        {
            IEnumerable<Commit> commits = persistence.GetFrom(snapshot.StreamId, snapshot.StreamRevision, maxRevision);
            PopulateStream(snapshot.StreamRevision + 1, maxRevision, commits);
            StreamRevision = snapshot.StreamRevision + _committed.Count;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual Guid StreamId { get; private set; }
        public virtual int StreamRevision { get; private set; }
        public virtual int CommitSequence { get; private set; }

        public virtual ICollection<EventMessage> CommittedEvents
        {
            get { return new ImmutableCollection<EventMessage>(_committed); }
        }

        public virtual IDictionary<string, object> CommittedHeaders
        {
            get { return _committedHeaders; }
        }

        public virtual ICollection<EventMessage> UncommittedEvents
        {
            get { return new ImmutableCollection<EventMessage>(_events); }
        }

        public virtual IDictionary<string, object> UncommittedHeaders
        {
            get { return _uncommittedHeaders; }
        }

        public virtual void Add(EventMessage uncommittedEvent)
        {
            if (uncommittedEvent == null || uncommittedEvent.Body == null)
            {
                return;
            }

            Logger.Debug(Resources.AppendingUncommittedToStream, StreamId);
            _events.Add(uncommittedEvent);
        }

        public virtual void CommitChanges(Guid commitId)
        {
            Logger.Debug(Resources.AttemptingToCommitChanges, StreamId);

            if (_identifiers.Contains(commitId))
            {
                throw new DuplicateCommitException();
            }

            if (!HasChanges())
            {
                return;
            }

            try
            {
                PersistChanges(commitId);
            }
            catch (ConcurrencyException)
            {
                Logger.Info(Resources.UnderlyingStreamHasChanged, StreamId);
                IEnumerable<Commit> commits = _persistence.GetFrom(StreamId, StreamRevision + 1, int.MaxValue);
                PopulateStream(StreamRevision + 1, int.MaxValue, commits);

                throw;
            }
        }

        public virtual void ClearChanges()
        {
            Logger.Debug(Resources.ClearingUncommittedChanges, StreamId);
            _events.Clear();
            _uncommittedHeaders.Clear();
        }

        protected void PopulateStream(int minRevision, int maxRevision, IEnumerable<Commit> commits)
        {
            foreach (var commit in commits ?? new Commit[0])
            {
                Logger.Verbose(Resources.AddingCommitsToStream, commit.CommitId, commit.Events.Count, StreamId);
                _identifiers.Add(commit.CommitId);

                CommitSequence = commit.CommitSequence;
                int currentRevision = commit.StreamRevision - commit.Events.Count + 1;
                if (currentRevision > maxRevision)
                {
                    return;
                }

                CopyToCommittedHeaders(commit);
                CopyToEvents(minRevision, maxRevision, currentRevision, commit);
            }
        }

        private void CopyToCommittedHeaders(Commit commit)
        {
            foreach (var key in commit.Headers.Keys)
            {
                _committedHeaders[key] = commit.Headers[key];
            }
        }

        private void CopyToEvents(int minRevision, int maxRevision, int currentRevision, Commit commit)
        {
            foreach (var @event in commit.Events)
            {
                if (currentRevision > maxRevision)
                {
                    Logger.Debug(Resources.IgnoringBeyondRevision, commit.CommitId, StreamId, maxRevision);
                    break;
                }

                if (currentRevision++ < minRevision)
                {
                    Logger.Debug(Resources.IgnoringBeforeRevision, commit.CommitId, StreamId, maxRevision);
                    continue;
                }

                _committed.Add(@event);
                StreamRevision = currentRevision - 1;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _disposed = true;
        }

        protected virtual bool HasChanges()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(Resources.AlreadyDisposed);
            }

            if (_events.Count > 0)
            {
                return true;
            }

            Logger.Warn(Resources.NoChangesToCommit, StreamId);
            return false;
        }

        protected virtual void PersistChanges(Guid commitId)
        {
            Commit attempt = BuildCommitAttempt(commitId);

            Logger.Debug(Resources.PersistingCommit, commitId, StreamId);
            _persistence.Commit(attempt);

            PopulateStream(StreamRevision + 1, attempt.StreamRevision, new[] {attempt});
            ClearChanges();
        }

        protected virtual Commit BuildCommitAttempt(Guid commitId)
        {
            Logger.Debug(Resources.BuildingCommitAttempt, commitId, StreamId);
            return new Commit(
                StreamId,
                StreamRevision + _events.Count,
                commitId,
                CommitSequence + 1,
                SystemTime.UtcNow,
                _uncommittedHeaders.ToDictionary(x => x.Key, x => x.Value),
                _events.ToList());
        }
    }
}