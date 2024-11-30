#region

using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using FluentAssertions;
using NEventStore.Persistence;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD.NUnit;

#endregion

#pragma warning disable 169 // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable S101 // Types should be named in PascalCase

namespace NEventStore.Tests;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

#if MSTEST
[TestClass]
#endif
public class when_creating_a_new_stream : using_persistence
{
    private IEventStream _stream;

    protected override void Because()
    {
        _stream = Store.CreateStream(streamId);
    }

    [Fact]
    public void should_return_a_new_stream()
    {
        _stream.Should().NotBeNull();
    }

    [Fact]
    public void should_return_a_stream_with_the_correct_stream_identifier()
    {
        _stream.StreamId.Should().Be(streamId);
    }

    [Fact]
    public void should_return_a_stream_with_a_zero_stream_revision()
    {
        _stream.StreamRevision.Should().Be(0);
    }

    [Fact]
    public void should_return_a_stream_with_a_zero_commit_sequence()
    {
        _stream.CommitSequence.Should().Be(0);
    }

    [Fact]
    public void should_return_a_stream_with_no_uncommitted_events()
    {
        _stream.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void should_return_a_stream_with_no_committed_events()
    {
        _stream.CommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void should_return_a_stream_with_empty_headers()
    {
        _stream.UncommittedHeaders.Should().BeEmpty();
    }
}

#if MSTEST
[TestClass]
#endif
public class when_opening_an_empty_stream_starting_at_revision_zero : using_persistence
{
    private IEventStream _stream;

    protected override void Context()
    {
        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, 0, 0)).Returns(new ICommit[0]);
    }

    protected override void Because()
    {
        _stream = Store.OpenStream(streamId, 0, 0);
    }

    [Fact]
    public void should_return_a_new_stream()
    {
        _stream.Should().NotBeNull();
    }

    [Fact]
    public void should_return_a_stream_with_the_correct_stream_identifier()
    {
        _stream.StreamId.Should().Be(streamId);
    }

    [Fact]
    public void should_return_a_stream_with_a_zero_stream_revision()
    {
        _stream.StreamRevision.Should().Be(0);
    }

    [Fact]
    public void should_return_a_stream_with_a_zero_commit_sequence()
    {
        _stream.CommitSequence.Should().Be(0);
    }

    [Fact]
    public void should_return_a_stream_with_no_uncommitted_events()
    {
        _stream.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void should_return_a_stream_with_no_committed_events()
    {
        _stream.CommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void should_return_a_stream_with_empty_headers()
    {
        _stream.UncommittedHeaders.Should().BeEmpty();
    }
}

#if MSTEST
[TestClass]
#endif
public class when_opening_an_empty_stream_starting_above_revision_zero : using_persistence
{
    private const int MinRevision = 1;
    private Exception _thrown;

    protected override void Context()
    {
        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, MinRevision, int.MaxValue))
            .Returns(Enumerable.Empty<ICommit>());
    }

    protected override void Because()
    {
        _thrown = Catch.Exception(() => Store.OpenStream(streamId, MinRevision));
    }

    [Fact]
    public void should_throw_a_StreamNotFoundException()
    {
        _thrown.Should().BeOfType<StreamNotFoundException>();
    }
}

#if MSTEST
[TestClass]
#endif
public class when_opening_a_populated_stream : using_persistence
{
    private const int MinRevision = 17;
    private const int MaxRevision = 42;
    private ICommit _committed;
    private IEventStream _stream;

    protected override void Context()
    {
        _committed = BuildCommitStub(1, MinRevision, 1);

        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, MinRevision, MaxRevision))
            .Returns(new[] { _committed });

        var hook = A.Fake<IPipelineHook>();
        A.CallTo(() => hook.Select(_committed)).Returns(_committed);
        PipelineHooks.Add(hook);
    }

    protected override void Because()
    {
        _stream = Store.OpenStream(streamId, MinRevision, MaxRevision);
    }

    [Fact]
    public void should_invoke_the_underlying_infrastructure_with_the_values_provided()
    {
        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, MinRevision, MaxRevision))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void should_provide_the_commits_to_the_selection_hooks()
    {
        PipelineHooks.ForEach(x => A.CallTo(() => x.Select(_committed)).MustHaveHappenedOnceExactly());
    }

    [Fact]
    public void should_return_an_event_stream_containing_the_correct_stream_identifer()
    {
        _stream.StreamId.Should().Be(streamId);
    }
}

#if MSTEST
[TestClass]
#endif
public class when_opening_a_populated_stream_from_a_snapshot : using_persistence
{
    private const int MaxRevision = int.MaxValue;
    private ICommit[] _committed;
    private Snapshot _snapshot;

    protected override void Context()
    {
        _snapshot = new Snapshot(streamId, 42, "snapshot");
        _committed = new[] { BuildCommitStub(1, 42, 0) };

        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, 42, MaxRevision)).Returns(_committed);
    }

    protected override void Because()
    {
        Store.OpenStream(_snapshot, MaxRevision);
    }

    [Fact]
    public void should_query_the_underlying_storage_using_the_revision_of_the_snapshot()
    {
        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, 42, MaxRevision)).MustHaveHappenedOnceExactly();
    }
}

#if MSTEST
[TestClass]
#endif
public class when_opening_a_stream_from_a_snapshot_that_is_at_the_revision_of_the_stream_head : using_persistence
{
    private const int HeadStreamRevision = 42;
    private const int HeadCommitSequence = 15;
    private EnumerableCounter<ICommit> _committed;
    private Snapshot _snapshot;
    private IEventStream _stream;

    protected override void Context()
    {
        _snapshot = new Snapshot(streamId, HeadStreamRevision, "snapshot");
        _committed = new EnumerableCounter<ICommit>(
            new[] { BuildCommitStub(1, HeadStreamRevision, HeadCommitSequence) });

        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, HeadStreamRevision, int.MaxValue))
            .Returns(_committed);
    }

    protected override void Because()
    {
        _stream = Store.OpenStream(_snapshot, int.MaxValue);
    }

    [Fact]
    public void should_return_a_stream_with_the_correct_stream_identifier()
    {
        _stream.StreamId.Should().Be(streamId);
    }

    [Fact]
    public void should_return_a_stream_with_revision_of_the_stream_head()
    {
        _stream.StreamRevision.Should().Be(HeadStreamRevision);
    }

    [Fact]
    public void should_return_a_stream_with_a_commit_sequence_of_the_stream_head()
    {
        _stream.CommitSequence.Should().Be(HeadCommitSequence);
    }

    [Fact]
    public void should_return_a_stream_with_no_committed_events()
    {
        _stream.CommittedEvents.Count.Should().Be(0);
    }

    [Fact]
    public void should_return_a_stream_with_no_uncommitted_events()
    {
        _stream.UncommittedEvents.Count.Should().Be(0);
    }

    [Fact]
    public void should_only_enumerate_the_set_of_commits_once()
    {
        _committed.GetEnumeratorCallCount.Should().Be(1);
    }
}

#if MSTEST
[TestClass]
#endif
public class when_reading_from_revision_zero : using_persistence
{
    protected override void Context()
    {
        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, 0, int.MaxValue))
            .Returns(Enumerable.Empty<ICommit>());
    }

    protected override void Because()
    {
        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        // This forces the enumeration of the commits.
        Store.GetFrom(streamId, 0, int.MaxValue).ToList();
    }

    [Fact]
    public void should_pass_a_revision_range_to_the_persistence_infrastructure()
    {
        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, 0, int.MaxValue)).MustHaveHappenedOnceExactly();
    }
}

#if MSTEST
[TestClass]
#endif
public class when_reading_up_to_revision_revision_zero : using_persistence
{
    private ICommit _committed;

    protected override void Context()
    {
        _committed = BuildCommitStub(1, 1, 1);

        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, 0, int.MaxValue)).Returns(new[] { _committed });
    }

    protected override void Because()
    {
        Store.OpenStream(streamId, 0, 0);
    }

    [Fact]
    public void should_pass_the_maximum_possible_revision_to_the_persistence_infrastructure()
    {
        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, 0, int.MaxValue)).MustHaveHappenedOnceExactly();
    }
}

#if MSTEST
[TestClass]
#endif
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
        thrown.Should().BeOfType<ArgumentNullException>();
    }
}

#if MSTEST
[TestClass]
#endif
public class when_reading_from_a_snapshot_up_to_revision_revision_zero : using_persistence
{
    private ICommit _committed;
    private Snapshot snapshot;

    protected override void Context()
    {
        snapshot = new Snapshot(streamId, 1, "snapshot");
        _committed = BuildCommitStub(1, 1, 1);

        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, snapshot.StreamRevision, int.MaxValue))
            .Returns(new[] { _committed });
    }

    protected override void Because()
    {
        Store.OpenStream(snapshot, 0);
    }

    [Fact]
    public void should_pass_the_maximum_possible_revision_to_the_persistence_infrastructure()
    {
        A.CallTo(() => Persistence.GetFrom(Bucket.Default, streamId, snapshot.StreamRevision, int.MaxValue))
            .MustHaveHappenedOnceExactly();
    }
}

#if MSTEST
[TestClass]
#endif
public class when_committing_a_null_attempt_back_to_the_stream : using_persistence
{
    private Exception thrown;

    protected override void Because()
    {
        thrown = Catch.Exception(() => ((ICommitEvents)Store).Commit(null));
    }

    [Fact]
    public void should_throw_an_ArgumentNullException()
    {
        thrown.Should().BeOfType<ArgumentNullException>();
    }
}

#if MSTEST
[TestClass]
#endif
public class when_committing_with_a_valid_and_populated_attempt_to_a_stream : using_persistence
{
    private CommitAttempt _populatedAttempt;
    private ICommit _populatedCommit;

    protected override void Context()
    {
        _populatedAttempt = BuildCommitAttemptStub(1, 1);

        A.CallTo(() => Persistence.Commit(_populatedAttempt))
            .ReturnsLazily((CommitAttempt attempt) =>
            {
                _populatedCommit = new Commit(attempt.BucketId,
                    attempt.StreamId,
                    attempt.StreamRevision,
                    attempt.CommitId,
                    attempt.CommitSequence,
                    attempt.CommitStamp,
                    1,
                    attempt.Headers,
                    attempt.Events);
                return _populatedCommit;
            });

        var hook = A.Fake<IPipelineHook>();
        A.CallTo(() => hook.PreCommit(_populatedAttempt)).Returns(true);

        PipelineHooks.Add(hook);
    }

    protected override void Because()
    {
        ((ICommitEvents)Store).Commit(_populatedAttempt);
    }

    [Fact]
    public void should_provide_the_commit_to_the_precommit_hooks()
    {
        PipelineHooks.ForEach(x => A.CallTo(() => x.PreCommit(_populatedAttempt)).MustHaveHappenedOnceExactly());
    }

    [Fact]
    public void should_provide_the_commit_attempt_to_the_configured_persistence_mechanism()
    {
        A.CallTo(() => Persistence.Commit(_populatedAttempt)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void should_provide_the_commit_to_the_postcommit_hooks()
    {
        PipelineHooks.ForEach(x => A.CallTo(() => x.PostCommit(_populatedCommit)).MustHaveHappenedOnceExactly());
    }
}

#if MSTEST
[TestClass]
#endif
public class when_a_precommit_hook_rejects_a_commit : using_persistence
{
    private CommitAttempt _attempt;
    private ICommit _commit;

    protected override void Context()
    {
        _attempt = BuildCommitAttemptStub(1, 1);
        _commit = BuildCommitStub(1, 1, 1);

        var hook = A.Fake<IPipelineHook>();
        A.CallTo(() => hook.PreCommit(_attempt)).Returns(false);

        PipelineHooks.Add(hook);
    }

    protected override void Because()
    {
        ((ICommitEvents)Store).Commit(_attempt);
    }

    [Fact]
    public void should_not_call_the_underlying_infrastructure()
    {
        A.CallTo(() => Persistence.Commit(_attempt)).MustNotHaveHappened();
    }

    [Fact]
    public void should_not_provide_the_commit_to_the_postcommit_hooks()
    {
        PipelineHooks.ForEach(x => A.CallTo(() => x.PostCommit(_commit)).MustNotHaveHappened());
    }
}

#if MSTEST
[TestClass]
#endif
public class when_accessing_the_underlying_persistence_with_pipeline_hooks : using_persistence
{
    protected override void Because()
    {
        PipelineHooks.Add(A.Fake<IPipelineHook>());
    }

    [Fact]
    public void should_return_a_reference_to_the_underlying_persistence_infrastructure_decorator()
    {
        Store.Advanced.Should().BeOfType<PipelineHooksAwarePersistanceDecorator>();
    }
}

public class when_accessing_the_underlying_persistence_without_pipeline_hooks : using_persistence
{
    [Fact]
    public void should_return_a_reference_to_the_underlying_persistence()
    {
        Store.Advanced.Should().BeOfType(Persistence.GetType());
    }
}

#if MSTEST
[TestClass]
#endif
public class when_disposing_the_event_store : using_persistence
{
    protected override void Because()
    {
        Store.Dispose();
    }

    [Fact]
    public void should_dispose_the_underlying_persistence()
    {
        A.CallTo(() => Persistence.Dispose()).MustHaveHappenedOnceExactly();
    }
}

public abstract class using_persistence : SpecificationBase
{
    private IPersistStreams persistence;
    private List<IPipelineHook> pipelineHooks;
    private OptimisticEventStore store;
    protected string streamId = Guid.NewGuid().ToString();
    protected IPersistStreams Persistence => persistence ?? (persistence = A.Fake<IPersistStreams>());
    protected List<IPipelineHook> PipelineHooks => pipelineHooks ?? (pipelineHooks = new List<IPipelineHook>());

    protected OptimisticEventStore Store
    {
        get { return store ?? (store = new OptimisticEventStore(Persistence, PipelineHooks.Select(x => x))); }
    }

    protected override void Cleanup()
    {
        streamId = Guid.NewGuid().ToString();
    }

    protected CommitAttempt BuildCommitAttemptStub(Guid commitId)
    {
        return new CommitAttempt(Bucket.Default, streamId, 1, commitId, 1, SystemTime.UtcNow, null, null);
    }

    protected ICommit BuildCommitStub(long checkpointToken, int streamRevision, int commitSequence)
    {
        var events = new[] { new EventMessage() }.ToList();
        return new Commit(Bucket.Default, streamId, streamRevision, Guid.NewGuid(), commitSequence, SystemTime.UtcNow,
            checkpointToken, null, events);
    }

    protected CommitAttempt BuildCommitAttemptStub(int streamRevision, int commitSequence)
    {
        var events = new[] { new EventMessage() };
        return new CommitAttempt(Bucket.Default, streamId, streamRevision, Guid.NewGuid(), commitSequence,
            SystemTime.UtcNow, null, events);
    }

    protected ICommit BuildCommitStub(long checkpointToken, Guid commitId, int streamRevision, int commitSequence)
    {
        var events = new[] { new EventMessage() }.ToList();
        return new Commit(Bucket.Default, streamId, streamRevision, commitId, commitSequence, SystemTime.UtcNow,
            checkpointToken, null, events);
    }
}

#pragma warning restore S101 // Types should be named in PascalCase
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore 169 // ReSharper enable InconsistentNaming