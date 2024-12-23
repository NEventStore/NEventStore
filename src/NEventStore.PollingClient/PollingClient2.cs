using NEventStore.Logging;
using NEventStore.Persistence;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace NEventStore.PollingClient
{
    /// <summary>
    /// This is the new polling client that does not depends on RX.
    /// </summary>
    public class PollingClient2 : IDisposable
    {
        /// <summary>
        /// Result of the handling of a commit.
        /// </summary>
        public enum HandlingResult
        {
            /// <summary>
            /// Move to the next commit.
            /// </summary>
            MoveToNext = 0,
            /// <summary>
            /// Retry the current commit.
            /// </summary>
            Retry = 1,
            /// <summary>
            /// Stop the polling client.
            /// </summary>
            Stop = 2,
        }

        private readonly ILogger _logger;

        private readonly Func<ICommit, HandlingResult> _commitCallback;

        private readonly IPersistStreams _persistStreams;

        private readonly Int32 _waitInterval;
        private readonly Thread _pollingThread;
        private readonly System.Timers.Timer _pollingWakeUpTimer;

        private Func<IEnumerable<ICommit>> _pollingFunc;

        private Int64 _checkpointToken;

        /// <summary>
        /// Created an NEventStore Polling Client
        /// </summary>
        /// <param name="persistStreams">the store to check</param>
        /// <param name="callback">callback to execute at each commit</param>
        /// <param name="waitInterval">Interval in Milliseconds to wait when the provider
        /// return no more commit and the next request</param>
        public PollingClient2(
            IPersistStreams persistStreams,
            Func<ICommit, HandlingResult> callback,
            Int32 waitInterval = 100)
        {
            _commitCallback = callback ?? throw new ArgumentNullException(nameof(callback), "Cannot use polling client without callback");
            _persistStreams = persistStreams ?? throw new ArgumentNullException(nameof(persistStreams), "PersistStreams cannot be null");

            _logger = LogFactory.BuildLogger(GetType());
            _waitInterval = waitInterval;
            _pollingWakeUpTimer = new System.Timers.Timer();
            _pollingWakeUpTimer.Elapsed += (sender, e) => WakeUpPoller();
            _pollingWakeUpTimer.Interval = _waitInterval;

            //Create polling thread
            _pollingThread = new Thread(InnerPollingLoop);
            _pollingThread.Start();

            LastActivityTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Tells the caller the last tick count when the last activity occurred. This is useful for the caller
        /// to setup Health check that verify if the polling client is really active and it is really loading new commits.
        /// This value is obtained with DateTime.UtcNow
        /// </summary>
        public DateTime LastActivityTimestamp { get; private set; }

        /// <summary>
        /// If the polling client encounter an exception it immediately retry, but we need to tell to the caller code
        /// that the last polling encounter an error. This is needed to detect a polling client stuck as an example
        /// with deserialization problems.
        /// </summary>
        public String LastPollingError { get; private set; }

        /// <summary>
        /// Start the polling client.
        /// </summary>
        public void StartFrom(Int64 checkpointToken = 0)
        {
            _checkpointToken = checkpointToken;
            ConfigurePollingFunction();
            StartPollingThread();
        }

        /// <summary>
        /// Start the polling client.
        /// </summary>
        public void StartFromBucket(string bucketId, Int64 checkpointToken = 0)
        {
            _checkpointToken = checkpointToken;
            ConfigurePollingFunction(bucketId);
            StartPollingThread();
        }

        /// <summary>
        /// Simply start the timer that will queue wake up tokens.
        /// </summary>
        private void StartPollingThread()
        {
            _pollingWakeUpTimer.Start();
        }

        /// <summary>
        /// Configure the polling function to get commits from the store.
        /// </summary>
        public void ConfigurePollingFunction(string bucketId = null)
        {
            if (bucketId == null)
                _pollingFunc = () => _persistStreams.GetFrom(_checkpointToken);
            else
                _pollingFunc = () => _persistStreams.GetFrom(bucketId, _checkpointToken);
        }

        /// <summary>
        /// Stop the polling client.
        /// </summary>
        public void Stop()
        {
            _stopRequest = true;
            _pollingWakeUpTimer?.Stop();

            WakeUpPoller();
        }

        /// <summary>
        /// Poll now.
        /// </summary>
        public void PollNow()
        {
            WakeUpPoller();
        }

        /// <summary>
        /// Add an object to wake up the polling client.
        /// </summary>
        private void WakeUpPoller()
        {
            //Avoid adding more than one wake up object.
            if (Interlocked.CompareExchange(ref _isPolling, 1, 0) == 0)
            {
                //If we have not a wake up object add one.
                if (_pollCollection.Count == 0)
                    _pollCollection.Add(new object());

                Interlocked.Exchange(ref _isPolling, 0);
            }
        }

        private int _isPolling;

        private Boolean _stopRequest;

        /// <summary>
        /// This blocking collection is used to Wake up the polling thread
        /// and to ensure that only the polling thread is polling from 
        /// event stream.
        /// </summary>
        private readonly BlockingCollection<Object> _pollCollection = new BlockingCollection<object>();

        private void InnerPollingLoop(object obj)
        {
            foreach (var pollRequest in _pollCollection.GetConsumingEnumerable())
            {
                //check stop request
                if (_stopRequest)
                    return;

                if (InnerPoll())
                    return;
            }
        }

        /// <summary>
        /// Added to avoid flooding of logging during polling.
        /// </summary>
        private DateTime _lastPollingErrorLogTimestamp = DateTime.MinValue;

        /// <summary>
        /// This is the inner polling function that does the polling and 
        /// returns true if there were errors that should stop the poller.
        /// </summary>
        /// <returns>Returns true if we need to stop the outer cycle.</returns>
        private bool InnerPoll()
        {
            LastActivityTimestamp = DateTime.UtcNow;
            if (_pollingFunc == null) return false;

            try
            {
                var commits = _pollingFunc();

                //if we have an error in the provider, the error will be thrown during enumeration
                foreach (var commit in commits)
                {
                    //We need to reset the error, because we read correctly a commit
                    LastPollingError = null;
                    LastActivityTimestamp = DateTime.UtcNow;
                    if (_stopRequest)
                    {
                        return true;
                    }
                    var result = _commitCallback(commit);
                    if (result == HandlingResult.Retry)
                    {
                        if (_logger.IsEnabled(LogLevel.Trace))
                        {
                            _logger.LogTrace("Commit callback ask retry for checkpointToken {0} - last dispatched {1}", commit.CheckpointToken, _checkpointToken);
                        }
                        break;
                    }
                    else if (result == HandlingResult.Stop)
                    {
                        Stop();
                        return true;
                    }
                    _checkpointToken = commit.CheckpointToken;
                }
                //if we reach here, we had no error contacting the persistence store.
                LastPollingError = null;
            }
            catch (Exception ex)
            {
                // These exceptions are expected to be transient
                LastPollingError = ex.Message;

                // These exceptions are expected to be transient, we log at maximum a log each minute.
                if (DateTime.UtcNow.Subtract(_lastPollingErrorLogTimestamp).TotalMinutes > 1)
                {
                    _logger.LogError(String.Format("Error during polling client {0}", ex.ToString()));
                    _lastPollingErrorLogTimestamp = DateTime.UtcNow;
                }

                //A transient reading error is possible, but we need to wait a little bit before retrying.
                Thread.Sleep(1000);
            }

            return false;
        }

        private Boolean _isDisposed;

        /// <summary>
        /// Dispose the polling client.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the polling client.
        /// </summary>
        public void Dispose(Boolean isDisposing)
        {
            if (_isDisposed) return;
            if (isDisposing)
            {
                Stop();
            }
            _isDisposed = true;
        }
    }
}
