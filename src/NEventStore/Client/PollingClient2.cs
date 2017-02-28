using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NEventStore.Logging;
using NEventStore.Persistence;

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
            if (_pollingThread != null)
                throw new PollingClientException("Polling client already started");
            _checkpointToken = checkpointToken;
            ConfigurePollingFunction();
            _pollingThread = new Thread(InnerPollingLoop);
            _pollingThread.Start();
        }

        public void ConfigurePollingFunction(string bucketId = null)
        {
            if (_pollingThread != null)
                throw new PollingClientException("Cannot configure when polling client already started polling");
            if (bucketId == null)
                _pollingFunc = () => _persistStreams.GetFrom(_checkpointToken);
            else
                _pollingFunc = () => _persistStreams.GetFrom(bucketId, _checkpointToken);
        }

        public void StartFromBucket(string bucketId, Int64 checkpointToken = 0)
        {
            if (_pollingThread != null)
                throw new PollingClientException("Polling client already started");
            _checkpointToken = checkpointToken;
            ConfigurePollingFunction(bucketId);
            _pollingThread = new Thread(InnerPollingLoop);
            _pollingThread.Start();
        }

        public void Stop()
        {
            _stopRequest = true;
        }

        public void PollNow()
        {
            //if (_pollingThread == null)
            //    throw new ArgumentException("You cannot call PollNow on a poller that is not started");
            //return Task<Boolean>.Factory.StartNew(InnerPoll);	 
			InnerPoll();
		}

        private int _isPolling = 0;

        private Boolean _stopRequest = false;

        private void InnerPollingLoop(object obj)
        {
            while (_stopRequest == false)
            {
                if (InnerPoll()) return;
                Thread.Sleep(_waitInterval);
            }
        }

        /// <summary>
        /// Added to avoid flooding of logging during polling.
        /// </summary>
        private DateTime _lastPollingErrorLogTimestamp = DateTime.MinValue;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns true if we need to stop the outer cycle.</returns>
        private bool InnerPoll()
        {
            if (Interlocked.CompareExchange(ref _isPolling, 1, 0) == 0)
            {
                _lastActivityTimestamp = DateTime.UtcNow;
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
                Interlocked.Exchange(ref _isPolling, 0);
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
