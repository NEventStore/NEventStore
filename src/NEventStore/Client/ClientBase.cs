namespace NEventStore.Client
{
    using NEventStore.Persistence;

    public abstract class ClientBase
    {
        private readonly IPersistStreams _persistStreams;

        protected ClientBase(IPersistStreams persistStreams)
        {
            _persistStreams = persistStreams;
        }

        protected IPersistStreams PersistStreams
        {
            get { return _persistStreams; }
        }

        public abstract IObserveCommits ObserveFrom(ICheckpoint checkpoint);

        public abstract IObserveCommits ObserveFromBegininng();
    }
}