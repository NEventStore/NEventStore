
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    public class OptimisticPipelineHookTests
    {
        public class when_committing_with_a_sequence_beyond_the_known_end_of_a_stream : using_commit_hooks
        {
            private const int HeadStreamRevision = 5;
            private const int HeadCommitSequence = 1;
            private const int ExpectedNextCommitSequence = HeadCommitSequence + 1;
            private const int BeyondEndOfStreamCommitSequence = ExpectedNextCommitSequence + 1;
            private ICommit _alreadyCommitted;
            private CommitAttempt _beyondEndOfStream;
            private Exception _thrown;

            protected override void Context()
            {
                _alreadyCommitted = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
                _beyondEndOfStream = BuildCommitAttemptStub(HeadStreamRevision + 1, BeyondEndOfStreamCommitSequence);

                Hook.PostCommit(_alreadyCommitted);
            }

            protected override void Because()
            {
                _thrown = Catch.Exception(() => Hook.PreCommit(_beyondEndOfStream));
            }

            [Fact]
            public void should_throw_a_PersistenceException()
            {
                _thrown.Should().BeOfType<StorageException>();
            }
        }

        public class when_committing_with_a_revision_beyond_the_known_end_of_a_stream : using_commit_hooks
        {
            private const int HeadCommitSequence = 1;
            private const int HeadStreamRevision = 1;
            private const int NumberOfEventsBeingCommitted = 1;
            private const int ExpectedNextStreamRevision = HeadStreamRevision + 1 + NumberOfEventsBeingCommitted;
            private const int BeyondEndOfStreamRevision = ExpectedNextStreamRevision + 1;
            private ICommit _alreadyCommitted;
            private CommitAttempt _beyondEndOfStream;
            private Exception _thrown;

            protected override void Context()
            {
                _alreadyCommitted = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
                _beyondEndOfStream = BuildCommitAttemptStub(BeyondEndOfStreamRevision, HeadCommitSequence + 1);

                Hook.PostCommit(_alreadyCommitted);
            }

            protected override void Because()
            {
                _thrown = Catch.Exception(() => Hook.PreCommit(_beyondEndOfStream));
            }

            [Fact]
            public void should_throw_a_PersistenceException()
            {
                _thrown.Should().BeOfType<StorageException>();
            }
        }

    public class when_committing_with_a_sequence_less_or_equal_to_the_most_recent_sequence_for_the_stream : using_commit_hooks
        {
            private const int HeadStreamRevision = 42;
            private const int HeadCommitSequence = 42;
            private const int DupliateCommitSequence = HeadCommitSequence;
            private CommitAttempt Attempt;
            private ICommit Committed;

            private Exception thrown;

            protected override void Context()
            {
                Committed = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
                Attempt = BuildCommitAttemptStub(HeadStreamRevision + 1, DupliateCommitSequence);

                Hook.PostCommit(Committed);
            }

            protected override void Because()
            {
                thrown = Catch.Exception(() => Hook.PreCommit(Attempt));
            }

            [Fact]
            public void should_throw_a_ConcurrencyException()
            {
                thrown.Should().BeOfType<ConcurrencyException>();
            }
        }

    public class when_committing_with_a_revision_less_or_equal_to_than_the_most_recent_revision_read_for_the_stream : using_commit_hooks
        {
            private const int HeadStreamRevision = 3;
            private const int HeadCommitSequence = 2;
            private const int DuplicateStreamRevision = HeadStreamRevision;
            private ICommit _committed;
            private CommitAttempt _failedAttempt;
            private Exception _thrown;

            protected override void Context()
            {
                _committed = BuildCommitStub(HeadStreamRevision, HeadCommitSequence);
                _failedAttempt = BuildCommitAttemptStub(DuplicateStreamRevision, HeadCommitSequence + 1);

                Hook.PostCommit(_committed);
            }

            protected override void Because()
            {
                _thrown = Catch.Exception(() => Hook.PreCommit(_failedAttempt));
            }

            [Fact]
            public void should_throw_a_ConcurrencyException()
            {
                _thrown.Should().BeOfType<ConcurrencyException>();
            }
        }

    public class when_committing_with_a_commit_sequence_less_than_or_equal_to_the_most_recent_commit_for_the_stream : using_commit_hooks
        {
            private const int DuplicateCommitSequence = 1;
            private CommitAttempt _failedAttempt;
            private ICommit _successfulAttempt;
            private Exception _thrown;

            protected override void Context()
            {
                _successfulAttempt = BuildCommitStub(1, DuplicateCommitSequence);
                _failedAttempt = BuildCommitAttemptStub(2, DuplicateCommitSequence);

                Hook.PostCommit(_successfulAttempt);
            }

            protected override void Because()
            {
                _thrown = Catch.Exception(() => Hook.PreCommit(_failedAttempt));
            }

            [Fact]
            public void should_throw_a_ConcurrencyException()
            {
                _thrown.Should().BeOfType<ConcurrencyException>();
            }
        }

    public class when_committing_with_a_stream_revision_less_than_or_equal_to_the_most_recent_commit_for_the_stream : using_commit_hooks
        {
            private const int DuplicateStreamRevision = 2;

            private CommitAttempt _failedAttempt;
            private ICommit _successfulAttempt;
            private Exception _thrown;

            protected override void Context()
            {
                _successfulAttempt = BuildCommitStub(DuplicateStreamRevision, 1);
                _failedAttempt = BuildCommitAttemptStub(DuplicateStreamRevision, 2);

                Hook.PostCommit(_successfulAttempt);
            }

            protected override void Because()
            {
                _thrown = Catch.Exception(() => Hook.PreCommit(_failedAttempt));
            }

            [Fact]
            public void should_throw_a_ConcurrencyException()
            {
                _thrown.Should().BeOfType<ConcurrencyException>();
            }
        }

        public class when_tracking_commits : SpecificationBase
        {
            private const int MaxStreamsToTrack = 2;
            private ICommit[] _trackedCommitAttempts;

            private OptimisticPipelineHook _hook;

            protected override void Context()
            {
                _trackedCommitAttempts = new[]
                {
                    BuildCommit(Guid.NewGuid(), Guid.NewGuid()),
                    BuildCommit(Guid.NewGuid(), Guid.NewGuid()),
                    BuildCommit(Guid.NewGuid(), Guid.NewGuid())
                };

                _hook = new OptimisticPipelineHook(MaxStreamsToTrack);
            }

            protected override void Because()
            {
                foreach (var commit in _trackedCommitAttempts)
                {
                    _hook.Track(commit);
                }
            }

            [Fact]
            public void should_only_contain_streams_explicitly_tracked()
            {
                ICommit untracked = BuildCommit(Guid.Empty, _trackedCommitAttempts[0].CommitId);
                _hook.Contains(untracked).Should().BeFalse();
            }

            [Fact]
            public void should_find_tracked_streams()
            {
                ICommit stillTracked = BuildCommit(_trackedCommitAttempts.Last().StreamId, _trackedCommitAttempts.Last().CommitId);
                _hook.Contains(stillTracked).Should().BeTrue();
            }

            [Fact]
            public void should_only_track_the_specified_number_of_streams()
            {
                ICommit droppedFromTracking = BuildCommit(
                    _trackedCommitAttempts.First().StreamId, _trackedCommitAttempts.First().CommitId);
                _hook.Contains(droppedFromTracking).Should().BeFalse();
            }

            private ICommit BuildCommit(Guid streamId, Guid commitId)
            {
                return BuildCommit(streamId.ToString(), commitId);
            }

            private ICommit BuildCommit(string streamId, Guid commitId)
            {
                return new Commit(Bucket.Default, streamId, 0, commitId, 0, SystemTime.UtcNow, new LongCheckpoint(0).Value, null, null);
            }
        }

        public class when_purging : SpecificationBase
        {
            private ICommit _trackedCommit;
            private OptimisticPipelineHook _hook;

            protected override void Context()
            {
                _trackedCommit = BuildCommit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
                _hook = new OptimisticPipelineHook();
                _hook.Track(_trackedCommit);
            }

            protected override void Because()
            {
                _hook.OnPurge();
            }

            [Fact]
            public void should_not_track_commit()
            {
                _hook.Contains(_trackedCommit).Should().BeFalse();
            }

            private ICommit BuildCommit(Guid bucketId, Guid streamId, Guid commitId)
            {
                return new Commit(bucketId.ToString(), streamId.ToString(), 0, commitId, 0, SystemTime.UtcNow,
                    new LongCheckpoint(0).Value, null, null);
            }
        }

        public class when_purging_a_bucket : SpecificationBase
        {
            private ICommit _trackedCommitBucket1;
            private ICommit _trackedCommitBucket2;
            private OptimisticPipelineHook _hook;

            protected override void Context()
            {
                _trackedCommitBucket1 = BuildCommit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
                _trackedCommitBucket2 = BuildCommit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
                _hook = new OptimisticPipelineHook();
                _hook.Track(_trackedCommitBucket1);
                _hook.Track(_trackedCommitBucket2);
            }

            protected override void Because()
            {
                _hook.OnPurge(_trackedCommitBucket1.BucketId);
            }

            [Fact]
            public void should_not_track_the_commit_in_bucket()
            {
                _hook.Contains(_trackedCommitBucket1).Should().BeFalse();
            }

            [Fact]
            public void should_track_the_commit_in_other_bucket()
            {
                _hook.Contains(_trackedCommitBucket2).Should().BeTrue();
            }

            private ICommit BuildCommit(Guid bucketId, Guid streamId, Guid commitId)
            {
                return new Commit(bucketId.ToString(), streamId.ToString(), 0, commitId, 0, SystemTime.UtcNow,
                    new LongCheckpoint(0).Value, null, null);
            }
        }

        public class when_deleting_a_stream : SpecificationBase
        {
            private ICommit _trackedCommit;
            private ICommit _trackedCommitDeleted;
            private OptimisticPipelineHook _hook;
            private readonly Guid _bucketId = Guid.NewGuid();
            private readonly Guid _streamIdDeleted = Guid.NewGuid();

            protected override void Context()
            {
                _trackedCommit = BuildCommit(_bucketId, Guid.NewGuid(), Guid.NewGuid());
                _trackedCommitDeleted = BuildCommit(_bucketId, _streamIdDeleted, Guid.NewGuid());
                _hook = new OptimisticPipelineHook();
                _hook.Track(_trackedCommit);
                _hook.Track(_trackedCommitDeleted);
            }

            protected override void Because()
            {
                _hook.OnDeleteStream(_trackedCommitDeleted.BucketId, _trackedCommitDeleted.StreamId);
            }

            [Fact]
            public void should_not_track_the_commit_in_the_deleted_stream()
            {
                _hook.Contains(_trackedCommitDeleted).Should().BeFalse();
            }

            [Fact]
            public void should_track_the_commit_that_is_not_in_the_deleted_stream()
            {
                _hook.Contains(_trackedCommit).Should().BeTrue();
            }

            private ICommit BuildCommit(Guid bucketId, Guid streamId, Guid commitId)
            {
                return new Commit(bucketId.ToString(), streamId.ToString(), 0, commitId, 0, SystemTime.UtcNow,
                    new LongCheckpoint(0).Value, null, null);
            }
        }

        public abstract class using_commit_hooks : SpecificationBase
        {
            protected readonly OptimisticPipelineHook Hook = new OptimisticPipelineHook();
            private readonly string _streamId = Guid.NewGuid().ToString();

            protected CommitAttempt BuildCommitStub(Guid commitId)
            {
                return new CommitAttempt(_streamId, 1, commitId, 1, SystemTime.UtcNow, null, null);
            }

            protected ICommit BuildCommitStub(int streamRevision, int commitSequence)
            {
                List<EventMessage> events = new[] {new EventMessage()}.ToList();
                return new Commit(Bucket.Default, _streamId, streamRevision, Guid.NewGuid(), commitSequence, SystemTime.UtcNow, new LongCheckpoint(0).Value, null, events);
            }

            protected CommitAttempt BuildCommitAttemptStub(int streamRevision, int commitSequence)
            {
                List<EventMessage> events = new[] {new EventMessage()}.ToList();
                return new CommitAttempt(Bucket.Default, _streamId, streamRevision, Guid.NewGuid(), commitSequence, SystemTime.UtcNow, null, events);
            }

            protected ICommit BuildCommitStub(Guid commitId, int streamRevision, int commitSequence)
            {
                List<EventMessage> events = new[] {new EventMessage()}.ToList();
                return new Commit(Bucket.Default, _streamId, streamRevision, commitId, commitSequence, SystemTime.UtcNow, new LongCheckpoint(0).Value, null, events);
            }
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169