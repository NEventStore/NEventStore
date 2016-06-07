using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NEventStore.Helpers;
using Xunit;
using Xunit.Should;

namespace NEventStore.Client
{
    public class CommitSequencerTests
    {
        private CommitSequencer sut;

        private Func<ICommit, PollingClient2.HandlingResult> callBack =
            c => PollingClient2.HandlingResult.MoveToNext;

        private int _outOfSequenceTimeoutInMilliseconds;

        public CommitSequencerTests()
        {
            _outOfSequenceTimeoutInMilliseconds = 2000;
            sut = new CommitSequencer(c => callBack(c), 0, _outOfSequenceTimeoutInMilliseconds);
        }

        [Fact]
        public void verify_check_sequential_missing_commit()
        {
            var result = sut.Handle(new TestICommit() {CheckpointToken = "1"});
            result.ShouldBe(PollingClient2.HandlingResult.MoveToNext);
            result = sut.Handle(new TestICommit() { CheckpointToken = "3" });
            result.ShouldBe(PollingClient2.HandlingResult.Retry);
        }

        [Fact]
        public void verify_timeout_on_missing_commit_not_elapsed()
        {
            DateTime start = DateTime.Now;
            var result = sut.Handle(new TestICommit() { CheckpointToken = "1" });
            result.ShouldBe(PollingClient2.HandlingResult.MoveToNext);
            using (DateTimeService.Override(start))
            {
                result = sut.Handle(new TestICommit() {CheckpointToken = "3"});
                result.ShouldBe(PollingClient2.HandlingResult.Retry);
            }
            using (DateTimeService.Override(start.AddMilliseconds(_outOfSequenceTimeoutInMilliseconds -100)))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = "3" });
                result.ShouldBe(PollingClient2.HandlingResult.Retry);
            }
        }

        [Fact]
        public void verify_idempotence_on_read_same_commit()
        {
            Int32 callBackCount = 0;
            callBack = c =>
            {
                callBackCount++;
                return PollingClient2.HandlingResult.MoveToNext;
            };
            var result = sut.Handle(new TestICommit() { CheckpointToken = "1" });
            result.ShouldBe(PollingClient2.HandlingResult.MoveToNext);
            callBackCount.ShouldBe(1);
            result = sut.Handle(new TestICommit() { CheckpointToken = "1" });
            result.ShouldBe(PollingClient2.HandlingResult.MoveToNext);
            callBackCount.ShouldBe(1);
        }

        [Fact]
        public void verify_timeout_on_missing_commit_then_next_commit()
        {
            DateTime start = DateTime.Now;
            var result = sut.Handle(new TestICommit() { CheckpointToken = "1" });
            result.ShouldBe(PollingClient2.HandlingResult.MoveToNext);
            using (DateTimeService.Override(start))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = "3" });
                result.ShouldBe(PollingClient2.HandlingResult.Retry);
            }
            using (DateTimeService.Override(start.AddMilliseconds(_outOfSequenceTimeoutInMilliseconds - 100)))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = "2" });
                result.ShouldBe(PollingClient2.HandlingResult.MoveToNext);
                result = sut.Handle(new TestICommit() { CheckpointToken = "3" });
                result.ShouldBe(PollingClient2.HandlingResult.MoveToNext);
            }

        }

        [Fact]
        public void verify_timeout_on_missing_commit_elapsed()
        {
            DateTime start = DateTime.Now;
            var result = sut.Handle(new TestICommit() { CheckpointToken = "1" });
            result.ShouldBe(PollingClient2.HandlingResult.MoveToNext);
            using (DateTimeService.Override(start))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = "3" });
                result.ShouldBe(PollingClient2.HandlingResult.Retry);
            }
            using (DateTimeService.Override(start.AddMilliseconds(_outOfSequenceTimeoutInMilliseconds + 100)))
            {
                result = sut.Handle(new TestICommit() { CheckpointToken = "3" });
                result.ShouldBe(PollingClient2.HandlingResult.MoveToNext);
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


            public string CheckpointToken { get; set; }
        }
    }
}
