#pragma warning disable IDE1006 // Naming Styles

#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentAssertions;
using NEventStore.Helpers;
using NEventStore.Persistence.AcceptanceTests.BDD.NUnit;
using NEventStore.PollingClient;
using NUnit.Framework;
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

namespace NEventStore.Tests.Client
{
#if MSTEST
    [TestClass]
#endif
#if NUNIT
    [TestFixture]
#endif
    public class CommitSequencerTests
    {
        private int _outOfSequenceTimeoutInMilliseconds;

        private CommitSequencer InitCommitSequencer(Func<ICommit, PollingClient2.HandlingResult> callBack = null)
        {
            if (callBack == null)
            {
                callBack = _ => PollingClient2.HandlingResult.MoveToNext;
            }
            _outOfSequenceTimeoutInMilliseconds = 2000;
            return new CommitSequencer(c => callBack(c), 0, _outOfSequenceTimeoutInMilliseconds);
        }

        [Fact]
        public void verify_check_sequential_missing_commit()
        {
            var sut = InitCommitSequencer();

            var result = sut.Handle(new TestICommit() { CheckpointToken = 1L });
            result.Should().Be(PollingClient2.HandlingResult.MoveToNext);
            result = sut.Handle(new TestICommit() { CheckpointToken = 3L });
            result.Should().Be(PollingClient2.HandlingResult.Retry);
        }

        [Fact]
        public void verify_timeout_on_missing_commit_not_elapsed()
        {
            var sut = InitCommitSequencer();

            DateTime start = DateTime.Now;
            var result = sut.Handle(new TestICommit() { CheckpointToken = 1L });
            result.Should().Be(PollingClient2.HandlingResult.MoveToNext);
            using (DateTimeService.Override(start))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = 3 });
                result.Should().Be(PollingClient2.HandlingResult.Retry);
            }
            using (DateTimeService.Override(start.AddMilliseconds(_outOfSequenceTimeoutInMilliseconds - 100)))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = 3 });
                result.Should().Be(PollingClient2.HandlingResult.Retry);
            }
        }

        [Fact]
        public void verify_idempotence_on_read_same_commit()
        {
            Int32 callBackCount = 0;
            var sut = InitCommitSequencer(_ =>
            {
                callBackCount++;
                return PollingClient2.HandlingResult.MoveToNext;
            });

            var result = sut.Handle(new TestICommit() { CheckpointToken = 1 });
            result.Should().Be(PollingClient2.HandlingResult.MoveToNext);
            callBackCount.Should().Be(1);
            result = sut.Handle(new TestICommit() { CheckpointToken = 1 });
            result.Should().Be(PollingClient2.HandlingResult.MoveToNext);
            callBackCount.Should().Be(1);
        }

        [Fact]
        public void verify_timeout_on_missing_commit_then_next_commit()
        {
            var sut = InitCommitSequencer();

            DateTime start = DateTime.Now;
            var result = sut.Handle(new TestICommit() { CheckpointToken = 1 });
            result.Should().Be(PollingClient2.HandlingResult.MoveToNext);
            using (DateTimeService.Override(start))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = 3 });
                result.Should().Be(PollingClient2.HandlingResult.Retry);
            }
            using (DateTimeService.Override(start.AddMilliseconds(_outOfSequenceTimeoutInMilliseconds - 100)))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = 2 });
                result.Should().Be(PollingClient2.HandlingResult.MoveToNext);
                result = sut.Handle(new TestICommit() { CheckpointToken = 3 });
                result.Should().Be(PollingClient2.HandlingResult.MoveToNext);
            }
        }

        [Fact]
        public void verify_timeout_on_missing_commit_elapsed()
        {
            var sut = InitCommitSequencer();

            DateTime start = DateTime.Now;
            var result = sut.Handle(new TestICommit() { CheckpointToken = 1 });
            result.Should().Be(PollingClient2.HandlingResult.MoveToNext);
            using (DateTimeService.Override(start))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = 3 });
                result.Should().Be(PollingClient2.HandlingResult.Retry);
            }
            using (DateTimeService.Override(start.AddMilliseconds(_outOfSequenceTimeoutInMilliseconds + 100)))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = 3 });
                result.Should().Be(PollingClient2.HandlingResult.MoveToNext);
            }
        }

        public class TestICommit : ICommit
        {
            public string BucketId
            {
                get { return ""; }
            }

            public string StreamId
            {
                get { return ""; }
            }

            public int StreamRevision
            {
                get { return 0; }
            }

            public Guid CommitId
            {
                get { return Guid.Empty; }
            }

            public int CommitSequence
            {
                get { return 0; }
            }

            public DateTime CommitStamp
            {
                get { return DateTimeService.Now; }
            }

            public IDictionary<string, object> Headers
            {
                get { return new ConcurrentDictionary<string, object>(); }
            }

            public ICollection<EventMessage> Events
            {
                get { return new List<EventMessage>(); }
            }

            public Int64 CheckpointToken { get; set; }
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
