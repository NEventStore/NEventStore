namespace NEventStore.Dispatcher
{
    using System;

    public class DelegateMessageDispatcher : IDispatchCommits
    {
        private readonly Action<Commit> _dispatch;

        public DelegateMessageDispatcher(Action<Commit> dispatch)
        {
            _dispatch = dispatch;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispatch(Commit commit)
        {
            _dispatch(commit);
        }

        protected virtual void Dispose(bool disposing)
        {
            // no op
        }
    }
}