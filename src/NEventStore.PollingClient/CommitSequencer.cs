using System;
using Microsoft.Extensions.Logging;
using NEventStore.Helpers;
using NEventStore.Logging;

namespace NEventStore.PollingClient
{
    public class CommitSequencer
    {
        private readonly ILogger _logger;

        private readonly Func<ICommit, PollingClient2.HandlingResult> _commitCallback;

        private Int64 _lastCommitRead;

        private readonly Int32 _outOfSequenceTimeoutInMilliseconds;

        public CommitSequencer(Func<ICommit, PollingClient2.HandlingResult> commitCallback, long lastCommitRead, int outOfSequenceTimeoutInMilliseconds)
        {
            _logger = LogFactory.BuildLogger(GetType());
            _commitCallback = commitCallback;
            _lastCommitRead = lastCommitRead;
            _outOfSequenceTimeoutInMilliseconds = outOfSequenceTimeoutInMilliseconds;
        }

        private DateTime? outOfSequenceTimestamp = null;

        public PollingClient2.HandlingResult Handle(ICommit commit)
        {
            var lc = commit.CheckpointToken;
            if (lc == _lastCommitRead + 1)
            {
                //is ok no need to resequence.
                return InnerHandleResult(commit, lc);
            }
            else if (_lastCommitRead >= lc)
            {
                _logger.LogWarning(String.Format("Wrong sequence in commit, last read {0} actual read {1}", _lastCommitRead, lc));
                return PollingClient2.HandlingResult.MoveToNext;
            }

            if (outOfSequenceTimestamp == null)
            {
                _logger.LogDebug("Sequencer found out of sequence, last dispatched {0} now dispatching {1}", _lastCommitRead, lc);
                outOfSequenceTimestamp = DateTimeService.Now;
            }
            else
            {
                var interval = DateTimeService.Now.Subtract(outOfSequenceTimestamp.Value);
                if (interval.TotalMilliseconds > _outOfSequenceTimeoutInMilliseconds)
                {
                    _logger.LogDebug("Sequencer out of sequence timeout after {0} ms, last dispatched {1} now dispatching {2}", interval.TotalMilliseconds, _lastCommitRead, lc);
                    return InnerHandleResult(commit, lc);
                }
                _logger.LogDebug("Sequencer still out of sequence from {0} ms, last dispatched {1} now dispatching {2}", interval.TotalMilliseconds, _lastCommitRead, lc);
            }

            return PollingClient2.HandlingResult.Retry;
        }

        private PollingClient2.HandlingResult InnerHandleResult(ICommit commit, Int64 lc)
        {
            var innerReturn = _commitCallback(commit);
            outOfSequenceTimestamp = null;
            if (innerReturn == PollingClient2.HandlingResult.MoveToNext)
            {
                _lastCommitRead = lc;
            }
            return innerReturn;
        }
    }
}
