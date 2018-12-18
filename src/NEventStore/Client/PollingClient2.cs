using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NEventStore.Logging;
using NEventStore.Persistence;
using System.Collections.Concurrent;

namespace NEventStore.Client
{
    /// <summary>
    /// This is the new polling client that does not depends on RX.
    /// </summary>
    public class PollingClient2 : IDisposable
    {
        public enum HandlingResult
        {
            MoveToNext = 0,
            Retry = 1,
            Stop = 2,
        }

        private readonly ILog _logger;

        private readonly Func<ICommit, HandlingResult> _commitCallback;

        private readonly IPersistStreams _persistStreams;

        private readonly Int32 _waitInterval;

        private DateTime _lastActivityTimestamp;

        private String _lastPollingError;

        private Thread _pollingThread;
        private System.Timers.Timer _pollingWakeUpTimer;

        private Func<IEnumerable<ICommit>> _pollingFunc;

        private Int64 _checkpointToken;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="persistStreams"></param>
        /// <param name="callback"></param>
        /// <param name="waitInterval">Interval in Milliseconds to wait when the provider
        /// return no more commit and the next request</param>
        public PollingClient2(IPersistStreams persistStreams, Func<ICommit, HandlingResult> callback, Int32 waitInterval = 100)
        {
            if (persistStreams == null)
                throw new ArgumentNullException("PersistStreams cannot be null", "persistStreams");
            if (callback == null)
                throw new ArgumentNullException("Cannot use polling client without callback", "callback");

            _logger = LogFactory.BuildLogger(GetType());
            _waitInterval = waitInterval;
            _pollingWakeUpTimer = new System.Timers.Timer();
            _pollingWakeUpTimer.Elapsed += (sender, e) => WakeUpPoller();
            _pollingWakeUpTimer.Interval = _waitInterval;

            //Create polling thread
            _pollingThread = new Thread(InnerPollingLoop);
            _pollingThread.Start();

            _commitCallback = callback;
            _persistStreams = persistStreams;
            _lastActivityTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Tells the caller the last tick count when the last activity occurred. This is useful for the caller
        /// to setup Health check that verify if the poller is really active and it is really loading new commits.
        /// This value is obtained with DateTime.UtcNow
        /// </summary>
        public DateTime LastActivityTimestamp { get { return _lastActivityTimestamp; } }

        /// <summary>
        /// If poller encounter an exception it immediately retry, but we need to tell to the caller code
        /// that the last polling encounter an error. This is needed to detect a poller stuck as an example
        /// with deserialization problems.
        /// </summary>
        public String LastPollingError { get { return _lastPollingError; } }

        public void StartFrom(Int64 checkpointToken = 0)
        {
            _checkpointToken = checkpointToken;
            ConfigurePollingFunction();
            StartPollingThread();
        }

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

        public void ConfigurePollingFunction(string bucketId = null)
        {
            if (bucketId == null)
                _pollingFunc = () => _persistStreams.GetFrom(_checkpointToken);
            else
                _pollingFunc = () => _persistStreams.GetFrom(bucketId, _checkpointToken);
        }

        public void Stop()
        {
            _stopRequest = true;
            if (_pollingWakeUpTimer != null) _pollingWakeUpTimer.Stop();
            WakeUpPoller();
        }

        public void PollNow()
        {
            WakeUpPoller();
        }

        /// <summary>
        /// Add an object to wake up the poller.
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

        private int _isPolling = 0;

        private Boolean _stopRequest = false;

        /// <summary>
        /// This blocking collection is used to Wake up the polling thread
        /// and to ensure that only the polling thread is polling from 
        /// eventstream.
        /// </summary>
        private BlockingCollection<Object> _pollCollection = new BlockingCollection<object>();

        private void InnerPollingLoop(object obj)
        {
            foreach (var pollRequest in _pollCollection.GetConsumingEnumerable())
            {
                //check stop request
                if (_stopRequest == true)
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
            _lastActivityTimestamp = DateTime.UtcNow;
            if (_pollingFunc == null) return false;

            try
            {
                var commits = _pollingFunc();

                //if we have an error in the provider, the error will be thrown during enumeration
                foreach (var commit in commits)
                {
                    //We need to reset the error, because we read correctly a commit
                    _lastPollingError = null;
                    _lastActivityTimestamp = DateTime.UtcNow;
                    if (_stopRequest)
                    {
                        return true;
                    }
                    var result = _commitCallback(commit);
                    if (result == HandlingResult.Retry)
                    {
                        _logger.Verbose("Commit callback ask retry for checkpointToken {0} - last dispatched {1}", commit.CheckpointToken, _checkpointToken);
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
                _lastPollingError = null;
            }
            catch (Exception ex)
            {
                // These exceptions are expected to be transient
                _lastPollingError = ex.Message;

                // These exceptions are expected to be transient, we log at maximum a log each minute.
                if (DateTime.UtcNow.Subtract(_lastPollingErrorLogTimestamp).TotalMinutes > 1)
                {
                    _logger.Error(String.Format("Error during polling client {0}", ex.ToString()));
                    _lastPollingErrorLogTimestamp = DateTime.UtcNow;
                }

                //A transient reading error is possible, but we need to wait a little bit before retrying.
                Thread.Sleep(1000);
            }

            return false;
        }

        private Boolean _isDisposed;

        public void Dispose()
        {
            Dispose(true);
        }

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
