namespace NEventStore.Diagnostics;
// PerformanceCounters are not cross platform

#if NET462
public class PerformanceCounterPersistenceEngine : IPersistStreams
{
    private readonly PerformanceCounters _counters;
    private readonly IPersistStreams _persistence;

    public PerformanceCounterPersistenceEngine(IPersistStreams persistence, string instanceName)
    {
        _persistence = persistence;
        _counters = new PerformanceCounters(instanceName);
    }

    public void Initialize()
    {
        _persistence.Initialize();
    }

    public ICommit Commit(CommitAttempt attempt)
    {
        Stopwatch clock = Stopwatch.StartNew();
        ICommit commit = _persistence.Commit(attempt);
        clock.Stop();
        _counters.CountCommit(attempt.Events.Count, clock.ElapsedMilliseconds);
        return commit;
    }

    public IEnumerable<ICommit> GetFromTo(string bucketId, DateTime start, DateTime end)
    {
        return _persistence.GetFromTo(bucketId, start, end);
    }

    public IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
    {
        return _persistence.GetFrom(bucketId, streamId, minRevision, maxRevision);
    }

    public IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
    {
        return _persistence.GetFrom(bucketId, start);
    }

    public IEnumerable<ICommit> GetFrom(Int64 checkpointToken)
    {
        return _persistence.GetFrom(checkpointToken);
    }

    public IEnumerable<ICommit> GetFromTo(Int64 from, Int64 to)
    {
        return _persistence.GetFromTo(from, to);
    }

    public IEnumerable<ICommit> GetFrom(string bucketId, Int64 checkpointToken)
    {
        return _persistence.GetFrom(bucketId, checkpointToken);
    }

    public IEnumerable<ICommit> GetFromTo(string bucketId, Int64 from, Int64 to)
    {
        return _persistence.GetFromTo(bucketId, from, to);
    }

    public bool AddSnapshot(ISnapshot snapshot)
    {
        bool result = _persistence.AddSnapshot(snapshot);
        if (result)
        {
            _counters.CountSnapshot();
        }

        return result;
    }

    public ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
    {
        return _persistence.GetSnapshot(bucketId, streamId, maxRevision);
    }

    public virtual IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
    {
        return _persistence.GetStreamsToSnapshot(bucketId, maxThreshold);
    }

    public virtual void Purge()
    {
        _persistence.Purge();
    }

    public void Purge(string bucketId)
    {
        _persistence.Purge(bucketId);
    }

    public void Drop()
    {
        _persistence.Drop();
    }

    public void DeleteStream(string bucketId, string streamId)
    {
        _persistence.DeleteStream(bucketId, streamId);
    }

    public bool IsDisposed
    {
        get { return _persistence.IsDisposed; }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PerformanceCounterPersistenceEngine()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _counters.Dispose();
        _persistence.Dispose();
    }

    public IPersistStreams UnwrapPersistenceEngine()
    {
        return _persistence;
    }
}
#endif