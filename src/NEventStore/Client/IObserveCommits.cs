namespace NEventStore.Client
{
    using System;

    public interface IObserveCommits : IObservable<Commit>, IDisposable
    {
        void Start();
    }
}