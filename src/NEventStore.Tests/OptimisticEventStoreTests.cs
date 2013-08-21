
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Moq;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class when_creating_a_new_stream : using_persistence
    {
        private IEventStream stream;

        protected override void Because()
        {
            stream = Store.CreateStream(streamId);
        }

        [Fact]
        public void should_return_a_new_stream()
        {
            stream.ShouldNotBeNull();
        }

        [Fact]
        public void should_return_a_stream_with_the_correct_stream_identifier()
        {
            stream.StreamId.ShouldBe(streamId);
        }

        [Fact]
        public void should_return_a_stream_with_a_zero_stream_revision()
        {
            stream.StreamRevision.ShouldBe(0);
        }

        [Fact]
        public void should_return_a_stream_with_a_zero_commit_sequence()
        {
            stream.CommitSequence.ShouldBe(0);
        }

        [Fact]
        public void should_return_a_stream_with_no_uncommitted_events()
        {
            stream.UncommittedEvents.ShouldBeEmpty();
        }

        [Fact]
        public void should_return_a_stream_with_no_committed_events()
        {
            stream.CommittedEvents.ShouldBeEmpty();
        }

        [Fact]
        public void should_return_a_stream_with_empty_headers()
        {
            stream.UncommittedHeaders.ShouldBeEmpty();
        }
    }

    public class when_opening_an_empty_stream_starting_at_revision_zero : using_persistence
    {
        private IEventStream stream;

        protected override void Context()
        {
            Persistence.Setup(x => x.GetFrom(Bucket.Default, streamId, 0, 0)).Returns(new Commit[0]);
        }

        protected override void Because()
        {
            stream = Store.OpenStream(streamId, 0, 0);
        }

        [Fact]
        public void should_return_a_new_stream()
        {
            stream.ShouldNotBeNull();
        }

        [Fact]
        public void should_return_a_stream_with_the_correct_stream_identifier()
        {
            stream.StreamId.ShouldBe(streamId);
        }

        [Fact]
        public void should_return_a_stream_with_a_zero_stream_revision()
        {
            stream.StreamRevision.ShouldBe(0);
        }

        [Fact]
        public void should_return_a_stream_with_a_zero_commit_sequence()
        {
            stream.CommitSequence.ShouldBe(0);
        }

        [Fact]
        public void should_return_a_stream_with_no_uncommitted_events()
        {
            stream.UncommittedEvents.ShouldBeEmpty();
        }

        [Fact]
        public void should_return_a_stream_with_no_committed_events()
        {
            stream.CommittedEvents.ShouldBeEmpty();
        }

        [Fact]
        public void should_return_a_stream_with_empty_headers()
        {
            stream.UncommittedHeaders.ShouldBeEmpty();
        }
    }

    public class when_opening_an_empty_stream_starting_above_revision_zero : using_persistence
    {
        private const int MinRevision = 1;
        private Exception thrown;

        protected override void Context()
        {
            Persistence.Setup(x => x.GetFrom(Bucket.Default, streamId, MinRevision, int.MaxValue)).Returns(new Commit[0]);
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => Store.OpenStream(streamId, MinRevision, int.MaxValue));
        }

        [Fact]
        public void should_throw_a_StreamNotFoundException()
        {
            thrown.ShouldBeInstanceOf<StreamNotFoundException>();
        }
    }

    public class when_opening_a_populated_stream : using_persistence
    {
        private const int MinRevision = 17;
        private const int MaxRevision = 42;
        private Commit Committed;
        private IEventStream stream;

        protected override void Context()
        {
            Committed = BuildCommitStub(MinRevision, 1);

            Persistence.Setup(x => x.GetFrom(Bucket.Default, streamId, MinRevision, MaxRevision)).Returns(new[] {Committed});
            PipelineHooks.Add(new Mock<IPipelineHook>());
            PipelineHooks[0].Setup(x => x.Select(Committed)).Returns(Committed);
        }

        protected override void Because()
        {
            stream = Store.OpenStream(streamId, MinRevision, MaxRevision);
        }

        [Fact]
        public void should_invoke_the_underlying_infrastructure_with_the_values_provided()
        {
            Persistence.Verify(x => x.GetFrom(Bucket.Default, streamId, MinRevision, MaxRevision), Times.Once());
        }

        [Fact]
        public void should_provide_the_commits_to_the_selection_hooks()
        {
            PipelineHooks.ForEach(x => x.Verify(hook => hook.Select(Committed), Times.Once()));
        }

        [Fact]
        public void should_return_an_event_stream_containing_the_correct_stream_identifer()
        {
            stream.StreamId.ShouldBe(streamId);
        }
    }

    public class when_opening_a_populated_stream_from_a_snapshot : using_persistence
    {
        private const int MaxRevision = int.MaxValue;
        private Commit[] Committed;
        private Snapshot snapshot;

        protected override void Context()
        {
            snapshot = new Snapshot(streamId, 42, "snapshot");
            Committed = new[] {BuildCommitStub(42, 0)};

            Persistence.Setup(x => x.GetFrom(Bucket.Default, streamId, 42, MaxRevision)).Returns(Committed);
        }

        protected override void Because()
        {
            Store.OpenStream(snapshot, MaxRevision);
        }

        [Fact]
        public void should_query_the_underlying_storage_using_the_revision_of_the_snapshot()
        {
            Persistence.Verify(x => x.GetFrom(Bucket.Default, streamId, 42, MaxRevision), Times.Once());
        }
    }

    public class when_opening_a_stream_from_a_snapshot_that_is_at_the_revision_of_the_stream_head : using_persistence
    {
        private const int HeadStreamRevision = 42;
        private const int HeadCommitSequence = 15;
        private EnumerableCounter Committed;
        private Snapshot snapshot;
        private IEventStream stream;

        protected override void Context()
        {
            snapshot = new Snapshot(streamId, HeadStreamRevision, "snapshot");
            Committed = new EnumerableCounter(
                new[] {BuildCommitStub(HeadStreamRevision, HeadCommitSequence)});

            Persistence.Setup(x => x.GetFrom(Bucket.Default, streamId, HeadStreamRevision, int.MaxValue)).Returns(Committed);
        }

        protected override void Because()
        {
            stream = Store.OpenStream(snapshot, int.MaxValue);
        }

        [Fact]
        public void should_return_a_stream_with_the_correct_stream_identifier()
        {
            stream.StreamId.ShouldBe(streamId);
        }

        [Fact]
        public void should_return_a_stream_with_revision_of_the_stream_head()
        {
            stream.StreamRevision.ShouldBe(HeadStreamRevision);
        }

        [Fact]
        public void should_return_a_stream_with_a_commit_sequence_of_the_stream_head()
        {
            stream.CommitSequence.ShouldBe(HeadCommitSequence);
        }

        [Fact]
        public void should_return_a_stream_with_no_committed_events()
        {
            stream.CommittedEvents.Count.ShouldBe(0);
        }

        [Fact]
        public void should_return_a_stream_with_no_uncommitted_events()
        {
            stream.UncommittedEvents.Count.ShouldBe(0);
        }

        [Fact]
        public void should_only_enumerate_the_set_of_commits_once()
        {
            Committed.GetEnumeratorCallCount.ShouldBe(1);
        }
    }

    public class when_reading_from_revision_zero : using_persistence
    {
        protected override void Context()
        {
            Persistence.Setup(x => x.GetFrom(Bucket.Default, streamId, 0, int.MaxValue)).Returns(new Commit[] { });
        }

        protected override void Because()
        {
            ((ICommitEvents) Store).GetFrom(streamId, 0, int.MaxValue).ToList();
        }

        [Fact]
        public void should_pass_a_revision_range_to_the_persistence_infrastructure()
        {
            Persistence.Verify(x => x.GetFrom(Bucket.Default, streamId, 0, int.MaxValue), Times.Once());
        }
    }

    public class when_reading_up_to_revision_revision_zero : using_persistence
    {
        private Commit Committed;

        protected override void Context()
        {
            Committed = BuildCommitStub(1, 1);

            Persistence
                .Setup(x => x.GetFrom(Bucket.Default, streamId, 0, int.MaxValue))
                .Returns(new[] {Committed});
        }

        protected override void Because()
        {
            Store.OpenStream(streamId, 0, 0);
        }

        [Fact]
        public void should_pass_the_maximum_possible_revision_to_the_persistence_infrastructure()
        {
            Persistence.Verify(x => x.GetFrom(Bucket.Default, streamId, 0, int.MaxValue), Times.Once());
        }
    }

    public class when_reading_from_a_null_snapshot : using_persistence
    {
        private Exception thrown;

        protected override void Because()
        {
            thrown = Catch.Exception(() => Store.OpenStream(null, int.MaxValue));
        }

        [Fact]
        public void should_throw_an_ArgumentNullException()
        {
            thrown.ShouldBeInstanceOf<ArgumentNullException>();
        }
    }

    public class when_reading_from_a_snapshot_up_to_revision_revision_zero : using_persistence
    {
        private Commit Committed;
        private Snapshot snapshot;

        protected override void Context()
        {
            snapshot = new Snapshot(streamId, 1, "snapshot");
            Committed = BuildCommitStub(1, 1);

            Persistence
                .Setup(x => x.GetFrom(Bucket.Default, streamId, snapshot.StreamRevision, int.MaxValue))
                .Returns(new[] {Committed});
        }

        protected override void Because()
        {
            Store.OpenStream(snapshot, 0);
        }

        [Fact]
        public void should_pass_the_maximum_possible_revision_to_the_persistence_infrastructure()
        {
            Persistence.Verify(x => x.GetFrom(Bucket.Default, streamId, snapshot.StreamRevision, int.MaxValue), Times.Once());
        }
    }

    public class when_committing_a_null_attempt_back_to_the_stream : using_persistence
    {
        private Exception thrown;

        protected override void Because()
        {
            thrown = Catch.Exception(() => ((ICommitEvents) Store).Commit(null));
        }

        [Fact]
        public void should_throw_an_ArgumentNullException()
        {
            thrown.ShouldBeInstanceOf<ArgumentNullException>();
        }
    }

    public class when_committing_with_an_unidentified_attempt_back_to_the_stream : using_persistence
    {
        private readonly Guid emptyIdentifier = Guid.Empty;
        private Exception thrown;
        private Commit unidentified;

        protected override void Context()
        {
            unidentified = BuildCommitStub(emptyIdentifier);
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => ((ICommitEvents) Store).Commit(unidentified));
        }

        [Fact]
        public void should_throw_an_ArgumentException()
        {
            thrown.ShouldBeInstanceOf<ArgumentException>();
        }
    }

    public class when_the_number_of_commits_is_greater_than_the_number_of_revisions : using_persistence
    {
        private const int StreamRevision = 1;
        private const int CommitSequence = 2; // should never be greater than StreamRevision.
        private Commit corrupt;
        private Exception thrown;

        protected override void Context()
        {
            corrupt = BuildCommitStub(StreamRevision, CommitSequence);
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => ((ICommitEvents) Store).Commit(corrupt));
        }

        [Fact]
        public void should_throw_a_StorageException()
        {
            thrown.ShouldBeInstanceOf<ArgumentException>();
        }
    }

    public class when_committing_with_a_nonpositive_commit_sequence_back_to_the_stream : using_persistence
    {
        private const int StreamRevision = 1;
        private const int InvalidCommitSequence = 0;
        private Commit invalidCommitSequence;
        private Exception thrown;

        protected override void Context()
        {
            invalidCommitSequence = BuildCommitStub(StreamRevision, InvalidCommitSequence);
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => ((ICommitEvents) Store).Commit(invalidCommitSequence));
        }

        [Fact]
        public void should_throw_an_ArgumentException()
        {
            thrown.ShouldBeInstanceOf<ArgumentException>();
        }
    }

    public class when_committing_with_a_non_positive_stream_revision_back_to_the_stream : using_persistence
    {
        private const int InvalidStreamRevision = 0;
        private const int CommitSequence = 1;
        private Commit invalidStreamRevision;
        private Exception thrown;

        protected override void Context()
        {
            invalidStreamRevision = BuildCommitStub(InvalidStreamRevision, CommitSequence);
        }

        protected override void Because()
        {
            thrown = Catch.Exception(() => ((ICommitEvents) Store).Commit(invalidStreamRevision));
        }

        [Fact]
        public void should_throw_an_ArgumentException()
        {
            thrown.ShouldBeInstanceOf<ArgumentException>();
        }
    }

    public class when_committing_an_empty_attempt_to_a_stream : using_persistence
    {
        private Commit attemptWithNoEvents;

        protected override void Context()
        {
            attemptWithNoEvents = BuildCommitStub(Guid.NewGuid());

            Persistence.Setup(x => x.Commit(attemptWithNoEvents));
        }

        protected override void Because()
        {
            ((ICommitEvents) Store).Commit(attemptWithNoEvents);
        }

        [Fact]
        public void should_drop_the_commit_provided()
        {
            Persistence.Verify(x => x.Commit(attemptWithNoEvents), Times.AtMost(0));
        }
    }

    public class when_committing_with_a_valid_and_populated_attempt_to_a_stream : using_persistence
    {
        private Commit populatedAttempt;

        protected override void Context()
        {
            populatedAttempt = BuildCommitStub(1, 1);

            Persistence.Setup(x => x.Commit(populatedAttempt));

            PipelineHooks.Add(new Mock<IPipelineHook>());
            PipelineHooks[0].Setup(x => x.PreCommit(populatedAttempt)).Returns(true);
            PipelineHooks[0].Setup(x => x.PostCommit(populatedAttempt));
        }

        protected override void Because()
        {
            ((ICommitEvents) Store).Commit(populatedAttempt);
        }

        [Fact]
        public void should_provide_the_commit_to_the_precommit_hooks()
        {
            PipelineHooks.ForEach(x => x.Verify(hook => hook.PreCommit(populatedAttempt), Times.Once()));
        }

        [Fact]
        public void should_provide_the_commit_attempt_to_the_configured_persistence_mechanism()
        {
            Persistence.Verify(x => x.Commit(populatedAttempt), Times.Once());
        }

        [Fact]
        public void should_provide_the_commit_to_the_postcommit_hooks()
        {
            PipelineHooks.ForEach(x => x.Verify(hook => hook.PostCommit(populatedAttempt), Times.Once()));
        }
    }

    public class when_a_precommit_hook_rejects_a_commit : using_persistence
    {
        private Commit attempt;

        protected override void Context()
        {
            attempt = BuildCommitStub(1, 1);

            PipelineHooks.Add(new Mock<IPipelineHook>());
            PipelineHooks[0].Setup(x => x.PreCommit(attempt)).Returns(false);
        }

        protected override void Because()
        {
            ((ICommitEvents) Store).Commit(attempt);
        }

        [Fact]
        public void should_not_call_the_underlying_infrastructure()
        {
            Persistence.Verify(x => x.Commit(attempt), Times.Never());
        }

        [Fact]
        public void should_not_provide_the_commit_to_the_postcommit_hooks()
        {
            PipelineHooks.ForEach(x => x.Verify(y => y.PostCommit(attempt), Times.Never()));
        }
    }

    public class when_accessing_the_underlying_persistence : using_persistence
    {
        public void should_return_a_reference_to_the_underlying_persistence_infrastructure_decorator()
        {
            Store.Advanced.ShouldBeInstanceOf<PipelineHooksAwarePersistanceDecorator>();
        }
    }

    public class when_disposing_the_event_store : using_persistence
    {
        protected override void Because()
        {
            Store.Dispose();
        }

        [Fact]
        public void should_dispose_the_underlying_persistence()
        {
            Persistence.Verify(x => x.Dispose(), Times.Once());
        }
    }

    public abstract class using_persistence : SpecificationBase
    {
        private Mock<IPersistStreams> persistence;
        private List<Mock<IPipelineHook>> pipelineHooks;
        private OptimisticEventStore store;
        protected string streamId = Guid.NewGuid().ToString();

        public Mock<IPersistStreams> Persistence
        {
            get { return persistence ?? (persistence = new Mock<IPersistStreams>()); }
        }

        public List<Mock<IPipelineHook>> PipelineHooks
        {
            get { return pipelineHooks ?? (pipelineHooks = new List<Mock<IPipelineHook>>()); }
        }

        public OptimisticEventStore Store
        {
            get { return store ?? (store = new OptimisticEventStore(Persistence.Object, PipelineHooks.Select(x => x.Object))); }
        }

        protected override void Cleanup()
        {
            streamId = Guid.NewGuid().ToString();
        }

        protected Commit BuildCommitStub(Guid commitId)
        {
            return new Commit(streamId, 1, commitId, 1, SystemTime.UtcNow, null, null);
        }

        protected Commit BuildCommitStub(int streamRevision, int commitSequence)
        {
            List<EventMessage> events = new[] {new EventMessage()}.ToList();
            return new Commit(streamId, streamRevision, Guid.NewGuid(), commitSequence, SystemTime.UtcNow, null, events);
        }

        protected Commit BuildCommitStub(Guid commitId, int streamRevision, int commitSequence)
        {
            List<EventMessage> events = new[] {new EventMessage()}.ToList();
            return new Commit(streamId, streamRevision, commitId, commitSequence, SystemTime.UtcNow, null, events);
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169