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

namespace NEventStore
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
        private IPipelineHookAsync? _hook1Async;
        private IPipelineHookAsync? _hook2Async;

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

            _hook1Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook1Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook1Async);

            _hook2Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook2Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook2Async);

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

        [Fact]
        public void should_pass_all_events_through_the_async_pipeline_hooks()
        {
            A.CallTo(() => _hook1Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_the_all_events_in_a_bucket_from_min_to_max_revision : using_underlying_persistence
    {
        private Commit? _commit;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;
        private IPipelineHookAsync? _hook1Async;
        private IPipelineHookAsync? _hook2Async;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            _hook1Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook1Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook1Async);

            _hook2Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook2Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook2Async);

            A.CallTo(() => persistence.GetFrom(Bucket.Default, _commit.StreamId, 0, int.MaxValue))
                .Returns([_commit]);
        }

        protected override void Because()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            // Forces enumeration of commits.
            Decorator.GetFrom(Bucket.Default, _commit!.StreamId, 0, int.MaxValue).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFrom(Bucket.Default, _commit!.StreamId, 0, int.MaxValue)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_async_pipeline_hooks()
        {
            A.CallTo(() => _hook1Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
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
        private IPipelineHookAsync? _hook1Async;
        private IPipelineHookAsync? _hook2Async;
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

            _hook1Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook1Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook1Async);

            _hook2Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook2Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook2Async);

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

        [Fact]
        public void should_pass_all_events_through_the_async_pipeline_hooks()
        {
            A.CallTo(() => _hook1Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
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
        private IPipelineHookAsync? _hook1Async;
        private IPipelineHookAsync? _hook2Async;
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

            _hook1Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook1Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook1Async);

            _hook2Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook2Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook2Async);

            A.CallTo(() => persistence.GetFromTo(_start, _end)).Returns([_commit]);
        }

        protected override void Because()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            // Forces enumeration of commits
            Decorator.GetFromTo(_start, _end).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFromTo(_start, _end)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_async_pipeline_hooks()
        {
            A.CallTo(() => _hook1Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
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
        private IPipelineHookAsync? _hook1Async;
        private IPipelineHookAsync? _hook2Async;
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

            _hook1Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook1Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook1Async);

            _hook2Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook2Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook2Async);

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

        [Fact]
        public void should_pass_all_events_through_the_async_pipeline_hooks()
        {
            A.CallTo(() => _hook1Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
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

        protected override void Because()
        {
            Decorator.Commit(_attempt!);
        }

        [Fact]
        public void should_dispose_the_underlying_persistence()
        {
            A.CallTo(() => persistence.Commit(_attempt!)).MustHaveHappenedOnceExactly();
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
        private IPipelineHookAsync? _hook1Async;
        private IPipelineHookAsync? _hook2Async;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            _hook1Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook1Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook1Async);

            _hook2Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook2Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook2Async);

            A.CallTo(() => persistence.GetFrom(0)).Returns([_commit]);
        }

        protected override void Because()
        {
            Decorator.GetFrom(0).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFrom(0)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_async_pipeline_hooks()
        {
            A.CallTo(() => _hook1Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
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
        private IPipelineHookAsync? _hook1Async;
        private IPipelineHookAsync? _hook2Async;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            _hook1Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook1Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook1Async);

            _hook2Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook2Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook2Async);

            A.CallTo(() => persistence.GetFrom(Bucket.Default, 0)).Returns([_commit]);
        }

        protected override void Because()
        {
            Decorator.GetFrom(Bucket.Default, 0).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFrom(Bucket.Default, 0)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_async_pipeline_hooks()
        {
            A.CallTo(() => _hook1Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _hook2Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging : using_underlying_persistence
    {
        private IPipelineHook? _hook;
        private IPipelineHookAsync? _hookAsync;

        protected override void Context()
        {
            _hook = A.Fake<IPipelineHook>();
            pipelineHooks.Add(_hook);

            _hookAsync = A.Fake<IPipelineHookAsync>();
            pipelineHooksAsync.Add(_hookAsync);
        }

        protected override void Because()
        {
            Decorator.Purge();
        }

        [Fact]
        public void should_call_the_pipeline_hook_purge()
        {
            A.CallTo(() => _hook!.OnPurge(null)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_call_the_async_pipeline_hook_purge()
        {
            A.CallTo(() => _hookAsync!.OnPurgeAsync(null, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging_a_bucket : using_underlying_persistence
    {
        private IPipelineHook? _hook;
        private IPipelineHookAsync? _hookAsync;
        private const string _bucketId = "Bucket";

        protected override void Context()
        {
            _hook = A.Fake<IPipelineHook>();
            pipelineHooks.Add(_hook);

            _hookAsync = A.Fake<IPipelineHookAsync>();
            pipelineHooksAsync.Add(_hookAsync);
        }

        protected override void Because()
        {
            Decorator.Purge(_bucketId);
        }

        [Fact]
        public void should_call_the_pipeline_hook_purge()
        {
            A.CallTo(() => _hook!.OnPurge(_bucketId)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_call_the_async_pipeline_hook_purge()
        {
            A.CallTo(() => _hookAsync!.OnPurgeAsync(_bucketId, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_deleting_a_stream : using_underlying_persistence
    {
        private IPipelineHook? _hook;
        private IPipelineHookAsync? _hookAsync;
        private const string _bucketId = "Bucket";
        private const string _streamId = "Stream";

        protected override void Context()
        {
            _hook = A.Fake<IPipelineHook>();
            pipelineHooks.Add(_hook);

            _hookAsync = A.Fake<IPipelineHookAsync>();
            pipelineHooksAsync.Add(_hookAsync);
        }

        protected override void Because()
        {
            Decorator.DeleteStream(_bucketId, _streamId);
        }

        [Fact]
        public void should_call_the_pipeline_hook_purge()
        {
            A.CallTo(() => _hook!.OnDeleteStream(_bucketId, _streamId)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_call_the_async_pipeline_hook_purge()
        {
            A.CallTo(() => _hookAsync!.OnDeleteStreamAsync(_bucketId, _streamId, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_the_all_events_from_checkpoint_with_filtering_PipelineHook_Sync : using_underlying_persistence
    {
        private ICommit? _commit;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;
        private IPipelineHookAsync? _hook1Async;
        private IPipelineHookAsync? _hook2Async;
        private List<ICommit>? _commits;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(null);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            _hook1Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook1Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook1Async);

            _hook2Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook2Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook2Async);

            A.CallTo(() => persistence.GetFrom(0)).Returns([_commit]);
        }

        protected override void Because()
        {
            _commits = Decorator.GetFrom(0).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFrom(0)).MustHaveHappenedOnceExactly();
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
        public void should_not_pass_the_events_through_the_first_async_pipeline_hooks()
        {
            A.CallTo(() => _hook1Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustNotHaveHappened();
        }

        [Fact]
        public void should_not_pass_the_events_through_the_second_async_pipeline_hooks()
        {
            A.CallTo(() => _hook2Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustNotHaveHappened();
        }

        [Fact]
        public void commit_list_should_be_empty()
        {
            _commits.Should().BeEmpty();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_the_all_events_from_checkpoint_with_filtering_PipelineHook_Async : using_underlying_persistence
    {
        private ICommit? _commit;
        private IPipelineHook? _hook1;
        private IPipelineHook? _hook2;
        private IPipelineHookAsync? _hook1Async;
        private IPipelineHookAsync? _hook2Async;
        private List<ICommit>? _commits;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 1, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.SelectCommit(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            _hook1Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook1Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(null));
            pipelineHooksAsync.Add(_hook1Async);

            _hook2Async = A.Fake<IPipelineHookAsync>();
            A.CallTo(() => _hook2Async.SelectCommitAsync(_commit, A<CancellationToken>.Ignored)).Returns(Task.FromResult<ICommit?>(_commit));
            pipelineHooksAsync.Add(_hook2Async);

            A.CallTo(() => persistence.GetFrom(0)).Returns([_commit]);
        }

        protected override void Because()
        {
            _commits = Decorator.GetFrom(0).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFrom(0)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_all_events_through_the_first_pipeline_hooks()
        {
            A.CallTo(() => _hook1!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_the_events_through_the_second_pipeline_hooks()
        {
            A.CallTo(() => _hook2!.SelectCommit(_commit!)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_pass_the_events_through_the_first_async_pipeline_hooks()
        {
            A.CallTo(() => _hook1Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void should_not_pass_the_events_through_the_second_async_pipeline_hooks()
        {
            A.CallTo(() => _hook2Async!.SelectCommitAsync(_commit!, A<CancellationToken>.Ignored)).MustNotHaveHappened();
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
        protected readonly List<IPipelineHookAsync> pipelineHooksAsync = [];
        protected readonly string streamId = Guid.NewGuid().ToString();

        public PipelineHooksAwarePersistStreamsDecorator Decorator
        {
            get { return decorator ??= new PipelineHooksAwarePersistStreamsDecorator(persistence, pipelineHooks.Select(x => x), pipelineHooksAsync.Select(x => x)); }
            set { decorator = value; }
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
// ReSharper enable InconsistentNaming
#pragma warning restore 169