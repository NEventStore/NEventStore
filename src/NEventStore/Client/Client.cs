namespace NEventStore.Client
{
    using System;
    using NEventStore.Persistence;

    public abstract class ClientBase
    {
        private readonly IPersistStreams _persistStreams;

        protected IPersistStreams PersistStreams
        {
            get { return _persistStreams; }
        }

        protected ClientBase(IPersistStreams persistStreams)
        {
            _persistStreams = persistStreams;
        }

        public abstract IObservable<Commit> ObserveFrom(int checkpoint);
    }

    public class PollingClient : ClientBase
    {
        private readonly int _pollingInterval;

        public PollingClient(IPersistStreams persistStreams, int pollingInterval)
            : base(persistStreams)
        {
            _pollingInterval = pollingInterval;
        }

        public override IObservable<Commit> ObserveFrom(int checkpoint)
        {
            return new PollingCommitObservable(PersistStreams, checkpoint);
        }

        private class PollingCommitObservable : IObservable<Commit>
        {
            public PollingCommitObservable(IPersistStreams persistStreams, int checkpoint)
            {
                
            } 

            public IDisposable Subscribe(IObserver<Commit> observer)
            {
                throw new NotImplementedException();
            }
        }
    }
}