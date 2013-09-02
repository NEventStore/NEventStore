namespace NEventStore.Dispatcher
{
    using System;

    public class DelegateMessageDispatcher : IDispatchCommits
    {
        private readonly Action<ICommit> _dispatch;

        public DelegateMessageDispatcher(Action<ICommit> dispatch)
        {
            _dispatch = dispatch;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispatch(ICommit commit)
        {
            _dispatch(commit);
        }

        protected virtual void Dispose(bool disposing)
        {
            // no op
        }
    }
}