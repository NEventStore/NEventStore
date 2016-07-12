using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NEventStore.Helpers;
using NEventStore.Logging;

namespace NEventStore.Client
{
    public class CommitSequencer
    {
        private readonly ILog _logger;

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
                return InnerHandleResult(commit, lc);
            }
            else if (_lastCommitRead >= lc)
            {
                _logger.Warn(String.Format("Wrong sequence in commit, last read {0} actual read {1}", _lastCommitRead, lc));
                return PollingClient2.HandlingResult.MoveToNext;
            }

            if (outOfSequenceTimestamp == null)
            {
                outOfSequenceTimestamp = DateTimeService.Now;
            }
            else
            {
                var interval = DateTimeService.Now.Subtract(outOfSequenceTimestamp.Value);
                if (interval.TotalMilliseconds > _outOfSequenceTimeoutInMilliseconds)
                {
                    return InnerHandleResult(commit, lc);
                }
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
