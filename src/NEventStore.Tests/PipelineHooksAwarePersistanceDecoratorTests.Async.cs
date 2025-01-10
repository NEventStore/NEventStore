using FakeItEasy;
using FluentAssertions;
using NEventStore.Persistence;
using NEventStore.Persistence.AcceptanceTests.BDD;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
#endif
#if NUNIT
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

#pragma warning disable 169
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

namespace NEventStore.Async
{
#if MSTEST
    [TestClass]
#endif
    public class when_disposing_the_decorator : using_underlying_persistence
    {
        protected override void Because()
        {
            Decorator.Dispose();
        }

        [Fact]
        public void should_dispose_the_underlying_persistence()
        {
            A.CallTo(() => persistence.Dispose()).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_the_all_events_in_a_bucket_from_date : using_underlying_persistence
    {
        private ICommit? _commit;
        private DateTime _date;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;

        protected override void Context()
        {
            _date = DateTime.Now;
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, _date, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFrom(Bucket.Default, _date)).Returns([_commit]);
        }

        protected override void Because()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            // Forces enumeration of commits.
            Decorator.GetFrom(Bucket.Default, _date).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFrom(Bucket.Default, _date)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_the_all_events_in_a_bucket_from_min_to_max_revision : using_underlying_persistence
    {
        private ICommit? _commit;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFromAsync(Bucket.Default, _commit.StreamId, 0, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
                .ReturnsLazily(async (string bucketId, string streamId, int minRevision, int maxRevision, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
                {
                    await asyncObserver.OnNextAsync(_commit, cancellation).ConfigureAwait(false);
                    await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
                });
        }

        protected override Task BecauseAsync()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            // Forces enumeration of commits.
            var streamObserver = new CommitStreamObserver();
            return Decorator.GetFromAsync(Bucket.Default, _commit!.StreamId, 0, int.MaxValue, streamObserver, CancellationToken.None);
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFromAsync(Bucket.Default, _commit!.StreamId, 0, int.MaxValue, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_all_events_in_a_bucket_from_date_to_date : using_underlying_persistence
    {
        private ICommit? _commit;
        private DateTime _end;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;
        private DateTime _start;

        protected override void Context()
        {
            _start = DateTime.Now;
            _end = DateTime.Now;
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFromTo(Bucket.Default, _start, _end)).Returns([_commit]);
        }

        protected override void Because()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            // Forces enumeration of commits
            Decorator.GetFromTo(Bucket.Default, _start, _end).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFromTo(Bucket.Default, _start, _end)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_all_events_in_all_buckets_from_checkpoint_to_checkpoint : using_underlying_persistence
    {
        private ICommit? _commit;
        private Int64 _end;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;
        private Int64 _start;

        protected override void Context()
        {
            _start = 0;
            _end = 1;
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFromToAsync(_start, _end, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
                .ReturnsLazily(async (long fromCheckpointToken, long toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
                {
                    await asyncObserver.OnNextAsync(_commit, cancellation).ConfigureAwait(false);
                    await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
                });
        }

        protected override Task BecauseAsync()
        {
            return Decorator.GetFromToAsync(_start, _end, new CommitStreamObserver(), CancellationToken.None);
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFromToAsync(_start, _end, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_all_events_in_a_bucket_from_checkpoint_to_checkpoint : using_underlying_persistence
    {
        private ICommit? _commit;
        private Int64 _end;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;
        private Int64 _start;

        protected override void Context()
        {
            _start = 0;
            _end = 1;
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFromToAsync(Bucket.Default, _start, _end, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
                .ReturnsLazily(async (string bucketId, long fromCheckpointToken, long toCheckpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
                {
                    await asyncObserver.OnNextAsync(_commit, cancellation).ConfigureAwait(false);
                    await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
                });
        }

        protected override Task BecauseAsync()
        {
            return Decorator.GetFromToAsync(Bucket.Default, _start, _end, new CommitStreamObserver(), CancellationToken.None);
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFromToAsync(Bucket.Default, _start, _end, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_committing : using_underlying_persistence
    {
        private CommitAttempt? _attempt;

        protected override void Context()
        {
            _attempt = new CommitAttempt(streamId, 1, Guid.NewGuid(), 1, DateTime.Now, null, [new EventMessage()]);
        }

        protected override Task BecauseAsync()
        {
            return Decorator.CommitAsync(_attempt!, CancellationToken.None);
        }

        [Fact]
        public void should_dispose_the_underlying_persistence()
        {
            A.CallTo(() => persistence.CommitAsync(_attempt!, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_the_all_events_from_checkpoint : using_underlying_persistence
    {
        private ICommit? _commit;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFromAsync(0, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
                .ReturnsLazily(async (long checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
                {
                    await asyncObserver.OnNextAsync(_commit, cancellation).ConfigureAwait(false);
                    await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
                });
        }

        protected override Task BecauseAsync()
        {
            return Decorator.GetFromAsync(0, new CommitStreamObserver(), CancellationToken.None);
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFromAsync(0, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_the_all_events_in_a_bucket_from_checkpoint : using_underlying_persistence
    {
        private ICommit? _commit;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFromAsync(Bucket.Default, 0, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
                .ReturnsLazily(async (string bucketId, long checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
                {
                    await asyncObserver.OnNextAsync(_commit, cancellation).ConfigureAwait(false);
                    await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
                });
        }

        protected override Task BecauseAsync()
        {
            return Decorator.GetFromAsync(Bucket.Default, 0, new CommitStreamObserver(), CancellationToken.None);
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFromAsync(Bucket.Default, 0, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging : using_underlying_persistence
    {
        private IPipelineHook? _hook;

        protected override void Context()
        {
            _hook = A.Fake<IPipelineHook>();
            pipelineHooks.Add(_hook);
        }

        protected override Task BecauseAsync()
        {
            return Decorator.PurgeAsync(CancellationToken.None);
        }

        [Fact]
        public void should_call_the_pipeline_hook_purge()
        {
            A.CallTo(() => _hook!.OnPurge(null)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging_a_bucket : using_underlying_persistence
    {
        private IPipelineHook? _hook;
        private const string _bucketId = "Bucket";

        protected override void Context()
        {
            _hook = A.Fake<IPipelineHook>();
            pipelineHooks.Add(_hook);
        }

        protected override Task BecauseAsync()
        {
            return Decorator.PurgeAsync(_bucketId, CancellationToken.None);
        }

        [Fact]
        public void should_call_the_pipeline_hook_purge()
        {
            A.CallTo(() => _hook!.OnPurge(_bucketId)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_deleting_a_stream : using_underlying_persistence
    {
        private IPipelineHook? _hook;
        private const string _bucketId = "Bucket";
        private const string _streamId = "Stream";

        protected override void Context()
        {
            _hook = A.Fake<IPipelineHook>();
            pipelineHooks.Add(_hook);
        }

        protected override Task BecauseAsync()
        {
            return Decorator.DeleteStreamAsync(_bucketId, _streamId, CancellationToken.None);
        }

        [Fact]
        public void should_call_the_pipeline_hook_purge()
        {
            A.CallTo(() => _hook!.OnDeleteStream(_bucketId, _streamId)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_the_all_events_from_checkpoint_with_filtering_PipelineHook : using_underlying_persistence
    {
        private ICommit? _commit;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;
        private IList<ICommit>? _commits;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(null);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFromAsync(0, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None))
                .ReturnsLazily(async (long checkpointToken, IAsyncObserver<ICommit> asyncObserver, CancellationToken cancellation) =>
                {
                    await asyncObserver.OnNextAsync(_commit, cancellation).ConfigureAwait(false);
                    await asyncObserver.OnCompletedAsync(cancellation).ConfigureAwait(false);
                });
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Decorator.GetFromAsync(0, observer, CancellationToken.None).ConfigureAwait(false);
            _commits = observer.Commits;
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFromAsync(0, A<IAsyncObserver<ICommit>>.Ignored, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_first_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_not_pass_the_events_through_the_second_pipeline_hooks()
        {
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustNotHaveHappened();
        }

        [Fact]
        public void commit_list_should_be_empty()
        {
            _commits.Should().BeEmpty();
        }
    }

    public abstract class using_underlying_persistence : SpecificationBase
    {
        private PipelineHooksAwarePersistStreamsDecorator? decorator;
        protected readonly IPersistStreams persistence = A.Fake<IPersistStreams>();
        protected readonly List<IPipelineHook> pipelineHooks = [];
        protected readonly string streamId = Guid.NewGuid().ToString();

        public PipelineHooksAwarePersistStreamsDecorator Decorator
        {
            get { return decorator ??= new PipelineHooksAwarePersistStreamsDecorator(persistence, pipelineHooks.Select(x => x)); }
            set { decorator = value; }
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
// ReSharper enable InconsistentNaming
#pragma warning restore 169