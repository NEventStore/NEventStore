﻿using Microsoft.Extensions.Logging;
using NEventStore.Helpers;
using NEventStore.Logging;
using System.Globalization;

namespace NEventStore.PollingClient
{
    /// <summary>
    /// A commit sequencer that can be used with <see cref="PollingClient2"/>
    /// </summary>
    public class CommitSequencer
    {
        private readonly ILogger _logger;

        private readonly Func<ICommit, PollingClient2.HandlingResult> _commitCallback;

        private Int64 _lastCommitRead;

        private readonly Int32 _outOfSequenceTimeoutInMilliseconds;

        /// <summary>
        ///   Initializes a new instance of the CommitSequencer class.
        /// </summary>
        public CommitSequencer(Func<ICommit, PollingClient2.HandlingResult> commitCallback, long lastCommitRead, int outOfSequenceTimeoutInMilliseconds)
        {
            _logger = LogFactory.BuildLogger(GetType());
            _commitCallback = commitCallback;
            _lastCommitRead = lastCommitRead;
            _outOfSequenceTimeoutInMilliseconds = outOfSequenceTimeoutInMilliseconds;
        }

        private DateTime? outOfSequenceTimestamp;

        /// <summary>
        ///  Handles the specified commit.
        /// </summary>
        public PollingClient2.HandlingResult Handle(ICommit commit)
        {
            var lc = commit.CheckpointToken;
            if (lc == _lastCommitRead + 1)
            {
                //is ok no need to re-sequence.
                return InnerHandleResult(commit, lc);
            }
            else if (_lastCommitRead >= lc)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(String.Format(CultureInfo.InvariantCulture, "Wrong sequence in commit, last read: {0} actual read: {1}", _lastCommitRead, lc));
                }
                return PollingClient2.HandlingResult.MoveToNext;
            }

            if (outOfSequenceTimestamp == null)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Sequencer found out of sequence, last dispatched: {LastDispatchedCheckpoint} now dispatching: {NowDispatchingCheckpoint}", _lastCommitRead, lc);
                }
                outOfSequenceTimestamp = DateTimeService.Now;
            }
            else
            {
                var interval = DateTimeService.Now.Subtract(outOfSequenceTimestamp.Value);
                if (interval.TotalMilliseconds > _outOfSequenceTimeoutInMilliseconds)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Sequencer out of sequence timeout after {TotalMilliseconds} ms, last dispatched: {LastDispatchedCheckpoint} now dispatching: {NowDispatchingCheckpoint}", interval.TotalMilliseconds, _lastCommitRead, lc);
                    }
                    return InnerHandleResult(commit, lc);
                }
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Sequencer still out of sequence from {TotalMilliseconds} ms, last dispatched: {LastDispatchedCheckpoint} now dispatching: {NowDispatchingCheckpoint}", interval.TotalMilliseconds, _lastCommitRead, lc);
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
