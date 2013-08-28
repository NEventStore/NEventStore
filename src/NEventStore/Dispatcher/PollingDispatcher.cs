/*namespace NEventStore.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NEventStore.Logging;
    using NEventStore.Persistence;

    public class PollingDispatcher : IScheduleDispatches
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (PollingDispatcher));
        private readonly Dictionary<string, List<TaskCompletionSource<Commit>>> _awaiting = new Dictionary<string, List<TaskCompletionSource<Commit>>>();
        private readonly ICheckpointHolder _checkpointHolder;
        private readonly IDispatchCommits _dispatcher;
        private readonly IPersistStreams _persistence;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;
        private Task _internalTask;

        public PollingDispatcher(IDispatchCommits dispatcher, IPersistStreams persistence, ICheckpointHolder checkpointHolder)
        {
            _dispatcher = dispatcher;
            _persistence = persistence;
            _checkpointHolder = checkpointHolder;

            Logger.Debug(Resources.InitializingPersistence);
            _persistence.Initialize();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void ScheduleDispatch(Commit commit)
        {
            Task<Commit> task = AwaitDispatchOf(commit);
            Task.WaitAll(task);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _disposed)
            {
                return;
            }

            Logger.Debug(Resources.ShuttingDownDispatchScheduler);
            _disposed = true;
            _dispatcher.Dispose();
            _persistence.Dispose();
        }

        public void StartPolling(TimeSpan interval)
        {
            if (_internalTask != null)
            {
                return;
            }
            _cancellationTokenSource = new CancellationTokenSource();
            Logger.Debug("polling started");
            _internalTask = Task.Factory.StartNew(x =>
                {
                    var token = (CancellationToken) x;
                    while (!token.IsCancellationRequested)
                    {
                        GetAndProcessNext();
                        Thread.Sleep(interval);
                    }
                },
                _cancellationTokenSource,
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private void GetAndProcessNext()
        {
            lock (this)
            {
                var items = _persistence.GetFrom(Guid.Parse(_checkpointHolder.GetCheckpoint()));
                foreach (var c in items)
                {
                    Dispatch(c);
                }
            }
        }

        public void StopPolling()
        {
            if (_internalTask == null)
            {
                return;
            }
            _cancellationTokenSource.Cancel();
        }

        public Task<Commit> AwaitDispatchOf(Commit commit)
        {
            TaskCompletionSource<Commit> source = CheckAwaiting(commit);
            return source.Task;
        }

        private TaskCompletionSource<Commit> CheckAwaiting(Commit commit)
        {
            //have we already done this before?
            if (commit.Checkpoint.IsAfter(_checkpointHolder.GetCheckpoint()))
            {
                var x = new TaskCompletionSource<Commit>();
                x.SetResult(commit);
            }

            lock (_awaiting)
            {
                List<TaskCompletionSource<Commit>> tasks;
                if (!_awaiting.TryGetValue(commit.Checkpoint.AsSavable(), out tasks))
                {
                    tasks = new List<TaskCompletionSource<Commit>>();
                    _awaiting.Add(commit.Checkpoint.AsSavable(), tasks);
                }
                var result = new TaskCompletionSource<Commit>();
                tasks.Add(result);
                return result;
            }
        }

        private void NotifyAsyncs(Commit commit)
        {
            lock (_awaiting)
            {
                List<TaskCompletionSource<Commit>> tasks;
                if (_awaiting.TryGetValue(commit.Checkpoint.AsSavable(), out tasks))
                {
                    tasks.ForEach(x => x.SetResult(commit));
                }
                _awaiting.Remove(commit.Checkpoint.AsSavable());
            }
        }

        private void NotifyAsyncsOfError(Commit commit, Exception exception)
        {
            lock (_awaiting)
            {
                List<TaskCompletionSource<Commit>> tasks;
                if (_awaiting.TryGetValue(commit.Checkpoint.AsSavable(), out tasks))
                {
                    tasks.ForEach(x => x.SetException(exception));
                }
                _awaiting.Remove(commit.CommitId);
            }
        }

        private void Dispatch(Commit commit)
        {
            try
            {
                Logger.Info(Resources.SchedulingDispatch, commit.CommitId);
                _dispatcher.Dispatch(commit);
                _checkpointHolder.SetCheckpoint(commit.CommitId);
                NotifyAsyncs(commit);
            }
            catch (Exception ex)
            {
                NotifyAsyncsOfError(commit, ex);
                Logger.Error(Resources.UnableToDispatch, _dispatcher.GetType(), commit.CommitId);
            }
        }
    }

    public interface ICheckpointHolder
    {
        void SetCheckpoint(string checkpoint);

        string GetCheckpoint();
    }
}*/