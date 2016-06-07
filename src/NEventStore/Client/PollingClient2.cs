using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NEventStore.Logging;
using NEventStore.Persistence;

namespace NEventStore.Client
{
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="persistStreams"></param>
        /// <param name="callback"></param>
        /// <param name="waitInterval">Interval in Milliseconds to wait when the provider
        /// return no more commit and the next request</param>
        public PollingClient2(IPersistStreams persistStreams, Func<ICommit, HandlingResult> callback, Int32 waitInterval = 100)
        {
            _logger = LogFactory.BuildLogger(GetType());
            _waitInterval = waitInterval;
            if (callback == null)
                throw new ArgumentException("Cannot use polling client without callback", "callback");
            _commitCallback = callback;
             _persistStreams = persistStreams;
        }

        private Thread _pollingThread;

        private Func<IEnumerable<ICommit>> _pollingFunc;

        private String _checkpointToken;

        public virtual void StartFrom(string checkpointToken = null)
        {
            if (_pollingThread != null) 
                throw  new ApplicationException("Polling client already started");
            _checkpointToken = checkpointToken;
            _pollingFunc = () => _persistStreams.GetFrom(_checkpointToken);
            _pollingThread = new Thread(InnerPollingLoop);
        }

        public virtual void StartFromBucket(string bucketId, string checkpointToken = null)
        {
            if (_pollingThread != null)
                throw new ApplicationException("Polling client already started");
            _checkpointToken = checkpointToken;
            _pollingFunc = () => _persistStreams.GetFrom(_checkpointToken, bucketId);
            _pollingThread = new Thread(InnerPollingLoop);
        }

        private int _isPolling = 0;

        private Boolean _stopRequest = false;
        private void InnerPollingLoop(object obj)
        {
            while (_stopRequest == false)
            {
                try
                {
                    var commits = _pollingFunc();

                    foreach (var commit in commits)
                    {
                        if (_stopRequest)
                        {
                            return;
                        }
                        var result = _commitCallback(commit);
                        if (result == HandlingResult.Retry)
                        {
                            break;
                        }
                        else if (result == HandlingResult.Stop)
                        {
                            Stop();
                            return;
                        }
                        _checkpointToken = commit.CheckpointToken;
                    }

                    Thread.Sleep(_waitInterval);
                }
                catch (Exception ex)
                {
                    // These exceptions are expected to be transient
                    _logger.Error(String.Format("Error during polling client {0}", ex.ToString()));
                }
            }
        }

        public virtual void Stop()
        {
            _stopRequest = true;
        }

        private Boolean _isDisposed;

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual void Dispose(Boolean isDisposing)
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
