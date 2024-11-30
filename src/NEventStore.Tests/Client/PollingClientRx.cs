using System;
using System.Reactive.Subjects;
using NEventStore.Persistence;
using NEventStore.PollingClient;

namespace NEventStore.Tests.Client;

/// <summary>
///     Represents a client that poll the storage for latest commits.
/// </summary>
public sealed class PollingClientRx
{
    private readonly PollingClient2 _pollingClient2;

    private readonly Subject<ICommit> _subject;

    private long _checkpointToObserveFrom;

    public PollingClientRx(
        IPersistStreams persistStreams,
        int waitInterval = 5000)
    {
        if (persistStreams == null) throw new ArgumentNullException(nameof(persistStreams));
        if (waitInterval <= 0) throw new ArgumentException("Must be greater than 0", nameof(waitInterval));
        _subject = new Subject<ICommit>();
        _pollingClient2 = new PollingClient2(persistStreams, c =>
            {
                _subject.OnNext(c);
                return PollingClient2.HandlingResult.MoveToNext;
            },
            waitInterval);
    }

    public IDisposable Subscribe(IObserver<ICommit> observer)
    {
        return _subject.Subscribe(observer);
    }

    public IObservable<ICommit> ObserveFrom(long checkpointToken = 0)
    {
        _checkpointToObserveFrom = checkpointToken;
        return _subject;
    }

    internal void Start()
    {
        _pollingClient2.StartFrom(_checkpointToObserveFrom);
    }

    internal void Dispose()
    {
        _pollingClient2.Dispose();
    }

    internal void StartFromBucket(string bucketId)
    {
        _pollingClient2.StartFromBucket(bucketId);
    }
}