namespace EventStore.Persistence.InMemoryPersistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class InMemoryPersistenceEngine : IPersistStreams
    {
        bool disposed;
        ICollection<Commit> commits;
        ICollection<StreamHead> heads;
        ICollection<Commit> undispatchedCommits;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || this.disposed)
                return;

            this.disposed = true;
        }

        public void Initialize()
        {
            commits = new LinkedList<Commit>();
            undispatchedCommits = new LinkedList<Commit>();
            heads = new LinkedList<StreamHead>();
        }

        public IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
        {
            lock (commits)
            {

                var snapshotCommit = commits.Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision && x.Snapshot != null)
                    .OrderByDescending(o => o.StreamRevision)
                    .Take(1)
                    .FirstOrDefault();

                long snapshotRevision = 0;
                if (snapshotCommit != null)
                    snapshotRevision = snapshotCommit.StreamRevision;

                return commits.Where(x => x.StreamId == streamId && x.StreamRevision >= snapshotRevision && x.StreamRevision <= maxRevision)
                    .ToList();
            }
        }


        public IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
        {
            lock (commits)
                return commits.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision).ToArray();
        }

        public void Persist(CommitAttempt uncommitted)
        {
            lock (commits)
            {
                var commit = uncommitted.ToCommit();

                if (commits.Contains(commit))
                    throw new DuplicateCommitException();
                if (commits.Any(c => c.StreamId == commit.StreamId && c.StreamRevision == commit.StreamRevision))
                    throw new ConcurrencyException();

                commits.Add(commit);

                lock (undispatchedCommits)
                    undispatchedCommits.Add(commit);


                lock (heads)
                {
                    var head = new StreamHead(commit.StreamId, null, commit.StreamRevision, 0);
                    if (heads.Contains(head))
                        heads.Remove(head);
                    heads.Add(head);
                }
            }
        }

        public IEnumerable<Commit> GetUndispatchedCommits()
        {
            lock (commits)
                return commits.Where(c => undispatchedCommits.Contains(c));
        }

        public void MarkCommitAsDispatched(Commit commit)
        {
            lock (undispatchedCommits)
                undispatchedCommits.Remove(commit);
        }

        public IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
        {
            lock (heads)
                return heads.Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold)
                        .Select(stream => new StreamHead(stream.StreamId,
                            stream.StreamName,
                            stream.HeadRevision,
                            stream.SnapshotRevision));

        }

        public void AddSnapshot(Guid streamId, long streamRevision, object snapshot)
        {
            lock (commits)
            {

                var commitToBeUpdated = commits.First(commit => commit.StreamId == streamId && commit.StreamRevision == streamRevision);

                commits.Remove(commitToBeUpdated);
                commits.Add(new Commit(commitToBeUpdated.StreamId,
                    commitToBeUpdated.CommitId,
                    commitToBeUpdated.StreamRevision,
                    commitToBeUpdated.CommitSequence,
                    commitToBeUpdated.Headers,
                    commitToBeUpdated.Events,
                    snapshot));

            }
            lock (heads)
            {
                var currentHead = heads.First(h => h.StreamId == streamId);

                heads.Remove(currentHead);
                heads.Add(new StreamHead(currentHead.StreamId, currentHead.StreamName, currentHead.HeadRevision, streamRevision));
            }


        }
    }
}