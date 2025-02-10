
#pragma warning disable 169 // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

using FluentAssertions;
using FakeItEasy;
using NEventStore.Persistence;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD;
using System.IO;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

namespace NEventStore.Async
{
#if MSTEST
    [TestClass]
#endif
    public class when_creating_a_new_stream : using_persistence
    {
        private IEventStream? _stream;

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
            _stream!.StreamId.Should().Be(streamId);
        }

        [Fact]
        public void should_return_a_stream_with_a_zero_stream_revision()
        {
            _stream!.StreamRevision.Should().Be(0);
        }

        [Fact]
        public void should_return_a_stream_with_a_zero_commit_sequence()
        {
            _stream!.CommitSequence.Should().Be(0);
        }

        [Fact]
        public void should_return_a_stream_with_no_uncommitted_events()
        {
            _stream!.UncommittedEvents.Should().BeEmpty();
        }

        [Fact]
        public void should_return_a_stream_with_no_committed_events()
        {
            _stream!.CommittedEvents.Should().BeEmpty();
        }

        [Fact]
        public void should_return_a_stream_with_empty_headers()
        {
            _stream!.UncommittedHeaders.Should().BeEmpty();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_opening_an_empty_stream_starting_at_revision_zero : using_persistence
    {
        private IEventStream? _stream;

        protected override void Context()
        {
            // read an empty stream!
            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, 0, 0, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
                .Returns(Task.CompletedTask);
        }

        protected override async Task BecauseAsync()
        {
            _stream = await Store.OpenStreamAsync(streamId, 0, 0, CancellationToken.None).ConfigureAwait(false);
        }

        [Fact]
        public void should_return_a_new_stream()
        {
            _stream.Should().NotBeNull();
        }

        [Fact]
        public void should_return_a_stream_with_the_correct_stream_identifier()
        {
            _stream!.StreamId.Should().Be(streamId);
        }

        [Fact]
        public void should_return_a_stream_with_a_zero_stream_revision()
        {
            _stream!.StreamRevision.Should().Be(0);
        }

        [Fact]
        public void should_return_a_stream_with_a_zero_commit_sequence()
        {
            _stream!.CommitSequence.Should().Be(0);
        }

        [Fact]
        public void should_return_a_stream_with_no_uncommitted_events()
        {
            _stream!.UncommittedEvents.Should().BeEmpty();
        }

        [Fact]
        public void should_return_a_stream_with_no_committed_events()
        {
            _stream!.CommittedEvents.Should().BeEmpty();
        }

        [Fact]
        public void should_return_a_stream_with_empty_headers()
        {
            _stream!.UncommittedHeaders.Should().BeEmpty();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_opening_an_empty_stream_starting_above_revision_zero : using_persistence
    {
        private const int MinRevision = 1;
        private Exception? _thrown;

        protected override void Context()
        {
            // read an empty stream!
            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, MinRevision, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
                .Returns(Task.CompletedTask);
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Store.OpenStreamAsync(streamId, MinRevision)).ConfigureAwait(false);
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
        private ICommit? _committed;
        private IEventStream? _stream;

        protected override void Context()
        {
            _committed = BuildCommitStub(1, MinRevision, 1);

            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, MinRevision, MaxRevision, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
              .ReturnsLazily(async (string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
              {
                  await asyncObserver.OnNextAsync(_committed, cancellation).ConfigureAwait(false);
                  await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
              });

            var hook = A.Fake<IPipelineHook>();
            A.CallTo(() => hook.SelectCommit(_committed)).Returns(_committed);
            PipelineHooks.Add(hook);

            var hookAsync = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => hookAsync.SelectCommitAsync(_committed, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_committed));
            PipelineHooksAsync.Add(hookAsync);
        }

        protected override async Task BecauseAsync()
        {
            _stream = await Store.OpenStreamAsync(streamId, MinRevision, MaxRevision).ConfigureAwait(false);
        }

        [Fact]
        public void should_invoke_the_underlying_infrastructure_with_the_values_provided()
        {
            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, MinRevision, MaxRevision, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_provide_the_commits_to_the_selection_hooks()
        {
            PipelineHooks.ForEach(x => A.CallTo(() => x.SelectCommit(_committed!)).MustHaveHappenedOnceExactly());
        }

        [Fact]
        public void should_provide_the_commits_to_the_async_selection_hooks()
        {
            PipelineHooksAsync.ForEach(x => A.CallTo(() => x.SelectCommitAsync(_committed!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly());
        }

        [Fact]
        public void should_return_an_event_stream_containing_the_correct_stream_identifier()
        {
            _stream!.StreamId.Should().Be(streamId);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_opening_a_populated_stream_from_a_snapshot : using_persistence
    {
        private const int MaxRevision = int.MaxValue;
        private ICommit[]? _committed;
        private Snapshot? _snapshot;

        protected override void Context()
        {
            _snapshot = new Snapshot(streamId, 42, "snapshot");
            _committed = [BuildCommitStub(1, 42, 0)];

            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, 42, MaxRevision, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
              .ReturnsLazily(async (string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
              {
                  foreach (var _commit in _committed)
                  {
                      await asyncObserver.OnNextAsync(_commit, cancellation).ConfigureAwait(false);
                  }
                  await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
              });
        }

        protected override Task BecauseAsync()
        {
            return Store.OpenStreamAsync(_snapshot!, MaxRevision, CancellationToken.None);
        }

        [Fact]
        public void should_query_the_underlying_storage_using_the_revision_of_the_snapshot()
        {
            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, 42, MaxRevision, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_opening_a_stream_from_a_snapshot_that_is_at_the_revision_of_the_stream_head : using_persistence
    {
        private const int HeadStreamRevision = 42;
        private const int HeadCommitSequence = 15;
        private EnumerableCounter<ICommit>? _committed;
        private Snapshot? _snapshot;
        private IEventStream? _stream;

        protected override void Context()
        {
            _snapshot = new Snapshot(streamId, HeadStreamRevision, "snapshot");
            _committed = new EnumerableCounter<ICommit>(
                [BuildCommitStub(1, HeadStreamRevision, HeadCommitSequence)]);

            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, HeadStreamRevision, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
              .ReturnsLazily(async (string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
              {
                  foreach (var _commit in _committed)
                  {
                      await asyncObserver.OnNextAsync(_commit, cancellation).ConfigureAwait(false);
                  }
                  await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
              });
        }

        protected override async Task BecauseAsync()
        {
            _stream = await Store.OpenStreamAsync(_snapshot!, int.MaxValue, CancellationToken.None).ConfigureAwait(false);
        }

        [Fact]
        public void should_return_a_stream_with_the_correct_stream_identifier()
        {
            _stream!.StreamId.Should().Be(streamId);
        }

        [Fact]
        public void should_return_a_stream_with_revision_of_the_stream_head()
        {
            _stream!.StreamRevision.Should().Be(HeadStreamRevision);
        }

        [Fact]
        public void should_return_a_stream_with_a_commit_sequence_of_the_stream_head()
        {
            _stream!.CommitSequence.Should().Be(HeadCommitSequence);
        }

        [Fact]
        public void should_return_a_stream_with_no_committed_events()
        {
            _stream!.CommittedEvents.Count.Should().Be(0);
        }

        [Fact]
        public void should_return_a_stream_with_no_uncommitted_events()
        {
            _stream!.UncommittedEvents.Count.Should().Be(0);
        }

        [Fact]
        public void should_only_enumerate_the_set_of_commits_once()
        {
            _committed!.GetEnumeratorCallCount.Should().Be(1);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_from_revision_zero : using_persistence
    {
        protected override void Context()
        {

            // read an empty stream!
            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, 0, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
                .Returns(Task.CompletedTask);
        }

        protected override Task BecauseAsync()
        {
            return Store.GetFromAsync(streamId, 0, int.MaxValue, new CommitStreamObserver(), CancellationToken.None);
        }

        [Fact]
        public void should_pass_a_revision_range_to_the_persistence_infrastructure()
        {
            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, 0, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_up_to_revision_revision_zero : using_persistence
    {
        private ICommit? _committed;

        protected override void Context()
        {
            _committed = BuildCommitStub(1, 1, 1);

            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, 0, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
              .ReturnsLazily(async (string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
              {
                  await asyncObserver.OnNextAsync(_committed, cancellation).ConfigureAwait(false);
                  await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
              });
        }

        protected override Task BecauseAsync()
        {
            return Store.OpenStreamAsync(streamId, 0, 0);
        }

        [Fact]
        public void should_pass_the_maximum_possible_revision_to_the_persistence_infrastructure()
        {
            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, 0, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_from_a_null_snapshot : using_persistence
    {
        private Exception? thrown;

        protected override async Task BecauseAsync()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            thrown = await Catch.ExceptionAsync(() => Store.OpenStreamAsync(null , int.MaxValue, CancellationToken.None)).ConfigureAwait(false);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
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
        private ICommit? _committed;
        private Snapshot? snapshot;

        protected override void Context()
        {
            snapshot = new Snapshot(streamId, 1, "snapshot");
            _committed = BuildCommitStub(1, 1, 1);

            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, snapshot.StreamRevision, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
              .ReturnsLazily(async (string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
              {
                  await asyncObserver.OnNextAsync(_committed, cancellation).ConfigureAwait(false);
                  await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
              });
        }

        protected override Task BecauseAsync()
        {
            return Store.OpenStreamAsync(snapshot!, 0, CancellationToken.None);
        }

        [Fact]
        public void should_pass_the_maximum_possible_revision_to_the_persistence_infrastructure()
        {
            A.CallTo(() => Persistence.GetFromAsync(Bucket.Default, streamId, snapshot!.StreamRevision, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_committing_a_null_attempt_back_to_the_stream : using_persistence
    {
        private Exception? thrown;

        protected override async Task BecauseAsync()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            thrown = await Catch.ExceptionAsync(() => Store.CommitAsync(null, CancellationToken.None)).ConfigureAwait(false);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
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
        private CommitAttempt? _populatedAttempt;
        private ICommit? _populatedCommit;

        protected override void Context()
        {
            _populatedAttempt = BuildCommitAttemptStub(1, 1);

            A.CallTo(() => Persistence.CommitAsync(_populatedAttempt, CancellationToken.None))
                .ReturnsLazily((CommitAttempt attempt, CancellationToken _) =>
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

            var hookAsync = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => hookAsync.PreCommitAsync(_populatedAttempt, A<CancellationToken>.Ignored)).Returns(Task.FromResult(true));
            PipelineHooksAsync.Add(hookAsync);
        }

        protected override Task BecauseAsync()
        {
            return Store.CommitAsync(_populatedAttempt!, CancellationToken.None);
        }

        [Fact]
        public void should_provide_the_commit_to_the_PreCommit_hooks()
        {
            PipelineHooks.ForEach(x => A.CallTo(() => x.PreCommit(_populatedAttempt!)).MustHaveHappenedOnceExactly());
        }

        [Fact]
        public void should_provide_the_commit_to_the_async_PreCommit_hooks()
        {
            PipelineHooksAsync.ForEach(x => A.CallTo(() => x.PreCommitAsync(_populatedAttempt!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly());
        }

        [Fact]
        public void should_provide_the_commit_attempt_to_the_configured_persistence_mechanism()
        {
            A.CallTo(() => Persistence.CommitAsync(_populatedAttempt!, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_provide_the_commit_to_the_PostCommit_hooks()
        {
            PipelineHooks.ForEach(x => A.CallTo(() => x.PostCommit(_populatedCommit!)).MustHaveHappenedOnceExactly());
        }

        [Fact]
        public void should_provide_the_commit_to_the_async_PostCommit_hooks()
        {
            PipelineHooksAsync.ForEach(x => A.CallTo(() => x.PostCommitAsync(_populatedCommit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly());
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_a_PreCommit_hook_rejects_a_commit : using_persistence
    {
        private CommitAttempt? _attempt;
        private ICommit? _commit;

        protected override void Context()
        {
            _attempt = BuildCommitAttemptStub(1, 1);
            _commit = BuildCommitStub(1, 1, 1);

            var hook = A.Fake<IPipelineHook>();
            A.CallTo(() => hook.PreCommit(_attempt)).Returns(false);
            PipelineHooks.Add(hook);

            var hookAsync = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => hookAsync.PreCommitAsync(_attempt, A<CancellationToken>.Ignored)).Returns(Task.FromResult(true));
            PipelineHooksAsync.Add(hookAsync);
        }

        protected override Task BecauseAsync()
        {
            return Store.CommitAsync(_attempt!, CancellationToken.None);
        }

        [Fact]
        public void should_not_call_the_underlying_infrastructure()
        {
            A.CallTo(() => Persistence.CommitAsync(_attempt!, CancellationToken.None)).MustNotHaveHappened();
        }

        [Fact]
        public void should_not_provide_the_commit_to_the_PostCommit_hooks()
        {
            PipelineHooks.ForEach(x => A.CallTo(() => x.PostCommit(_commit!)).MustNotHaveHappened());
        }

        [Fact]
        public void should_not_provide_the_commit_to_the_async_PostCommit_hooks()
        {
            PipelineHooksAsync.ForEach(x => A.CallTo(() => x.PostCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustNotHaveHappened());
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_an_async_PreCommit_hook_rejects_a_commit : using_persistence
    {
        private CommitAttempt? _attempt;
        private ICommit? _commit;

        protected override void Context()
        {
            _attempt = BuildCommitAttemptStub(1, 1);
            _commit = BuildCommitStub(1, 1, 1);

            var hook = A.Fake<IPipelineHook>();
            A.CallTo(() => hook.PreCommit(_attempt)).Returns(true);
            PipelineHooks.Add(hook);

            var hookAsync = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => hookAsync.PreCommitAsync(_attempt, A<CancellationToken>.Ignored)).Returns(Task.FromResult(false));
            PipelineHooksAsync.Add(hookAsync);
        }

        protected override Task BecauseAsync()
        {
            return Store.CommitAsync(_attempt!, CancellationToken.None);
        }

        [Fact]
        public void should_not_call_the_underlying_infrastructure()
        {
            A.CallTo(() => Persistence.CommitAsync(_attempt!, CancellationToken.None)).MustNotHaveHappened();
        }

        [Fact]
        public void should_not_provide_the_commit_to_the_PostCommit_hooks()
        {
            PipelineHooks.ForEach(x => A.CallTo(() => x.PostCommit(_commit!)).MustNotHaveHappened());
        }

        [Fact]
        public void should_not_provide_the_commit_to_the_async_PostCommit_hooks()
        {
            PipelineHooksAsync.ForEach(x => A.CallTo(() => x.PostCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustNotHaveHappened());
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
            Store.Advanced.Should().BeOfType<PipelineHooksAwarePersistStreamsDecorator>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_accessing_the_underlying_persistence_with_async_pipeline_hooks : using_persistence
    {
        protected override void Because()
        {
            PipelineHooksAsync.Add(A.Fake<IPipelineHookAsync>());
        }

        [Fact]
        public void should_return_a_reference_to_the_underlying_persistence_infrastructure_decorator()
        {
            Store.Advanced.Should().BeOfType<PipelineHooksAwarePersistStreamsDecorator>();
        }
    }

#if MSTEST
    [TestClass]
#endif
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
        private IPersistStreams? persistence;

        private List<IPipelineHook>? pipelineHooks;
        private List<IPipelineHookAsync>? pipelineHooksAsync;
        private OptimisticEventStore? store;
        protected string streamId = Guid.NewGuid().ToString();

        protected IPersistStreams Persistence
        {
            get { return persistence ??= A.Fake<IPersistStreams>(); }
        }

        protected List<IPipelineHook> PipelineHooks
        {
            get { return pipelineHooks ??= []; }
        }

        protected List<IPipelineHookAsync> PipelineHooksAsync
        {
            get { return pipelineHooksAsync ??= []; }
        }

        protected OptimisticEventStore Store
        {
            get { return store ??= new OptimisticEventStore(Persistence, PipelineHooks.Select(x => x), PipelineHooksAsync.Select(x => x)); }
        }

        protected override void Cleanup()
        {
            streamId = Guid.NewGuid().ToString();
        }

        protected CommitAttempt BuildCommitAttemptStub(Guid commitId)
        {
            return new CommitAttempt(Bucket.Default, streamId, 1, commitId, 1, SystemTime.UtcNow, null, []);
        }

        protected ICommit BuildCommitStub(long checkpointToken, int streamRevision, int commitSequence)
        {
            List<EventMessage> events = new[] { new EventMessage() }.ToList();
            return new Commit(Bucket.Default, streamId, streamRevision, Guid.NewGuid(), commitSequence, SystemTime.UtcNow, checkpointToken, null, events);
        }

        protected CommitAttempt BuildCommitAttemptStub(int streamRevision, int commitSequence)
        {
            var events = new[] { new EventMessage() };
            return new CommitAttempt(Bucket.Default, streamId, streamRevision, Guid.NewGuid(), commitSequence, SystemTime.UtcNow, null, events);
        }

        protected ICommit BuildCommitStub(long checkpointToken, Guid commitId, int streamRevision, int commitSequence)
        {
            List<EventMessage> events = new[] { new EventMessage() }.ToList();
            return new Commit(Bucket.Default, streamId, streamRevision, commitId, commitSequence, SystemTime.UtcNow, checkpointToken, null, events);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore 169 // ReSharper enable InconsistentNaming