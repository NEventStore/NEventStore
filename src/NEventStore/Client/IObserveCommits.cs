namespace NEventStore.Client
{
    using System;
    using System.Threading.Tasks;

    public interface IObserveCommits : IObservable<Commit>, IDisposable
    {
        Task Start();
    }
}