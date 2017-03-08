
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FakeItEasy;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests.BDD;
#if MSTEST
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using FluentAssertions;
#endif
#if NUNIT
	using NUnit.Framework;	
#endif
#if XUNIT
	using Xunit;
	sing Xunit.Should;
#endif

	public class PipelineHooksAwarePersistenceDecoratorTests
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
                A.CallTo(() => persistence.Dispose()).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

#if MSTEST
		[TestClass]
#endif
		public class when_reading_the_all_events_from_date : using_underlying_persistence
        {
            private ICommit _commit;
            private DateTime _date;
            private IPipelineHook _hook1;
            private IPipelineHook _hook2;

            protected override void Context()
            {
                _date = DateTime.Now;
                _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 0, null, null);

                _hook1 = A.Fake<IPipelineHook>();
                A.CallTo(() => _hook1.Select(_commit)).Returns(_commit);
                pipelineHooks.Add(_hook1);

                _hook2 = A.Fake<IPipelineHook>();
                A.CallTo(() => _hook2.Select(_commit)).Returns(_commit);
                pipelineHooks.Add(_hook2);

                A.CallTo(() => persistence.GetFrom(Bucket.Default, _date)).Returns(new List<ICommit> {_commit});
            }

            protected override void Because()
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                // Forces enumeration of commits.
                Decorator.GetFrom(_date).ToList();
            }

            [Fact]
            public void should_call_the_underlying_persistence_to_get_events()
            {
                A.CallTo(() => persistence.GetFrom(Bucket.Default, _date)).MustHaveHappened(Repeated.Exactly.Once);
            }

            [Fact]
            public void should_pass_all_events_through_the_pipeline_hooks()
            {
                A.CallTo(() => _hook1.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
                A.CallTo(() => _hook2.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

#if MSTEST
		[TestClass]
#endif
		public class when_getting_the_all_events_from_min_to_max_revision : using_underlying_persistence
        {
            private ICommit _commit;
            private DateTime _date;
            private IPipelineHook _hook1;
            private IPipelineHook _hook2;

            protected override void Context()
            {
                _date = DateTime.Now;
                _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 0, null, null);

                _hook1 = A.Fake<IPipelineHook>();
                A.CallTo(() => _hook1.Select(_commit)).Returns(_commit);
                pipelineHooks.Add(_hook1);

                _hook2 = A.Fake<IPipelineHook>();
                A.CallTo(() => _hook2.Select(_commit)).Returns(_commit);
                pipelineHooks.Add(_hook2);

                A.CallTo(() => persistence.GetFrom(Bucket.Default, _commit.StreamId, 0, int.MaxValue))
                    .Returns(new List<ICommit> { _commit });
            }

            protected override void Because()
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                // Forces enumeration of commits.
                Decorator.GetFrom(Bucket.Default, _commit.StreamId, 0, int.MaxValue).ToList();
            }

            [Fact]
            public void should_call_the_underlying_persistence_to_get_events()
            {
                A.CallTo(() => persistence.GetFrom(Bucket.Default, _commit.StreamId, 0, int.MaxValue)).MustHaveHappened(Repeated.Exactly.Once);
            }

            [Fact]
            public void should_pass_all_events_through_the_pipeline_hooks()
            {
                A.CallTo(() => _hook1.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
                A.CallTo(() => _hook2.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

#if MSTEST
		[TestClass]
#endif
		public class when_getting_all_events_from_to : using_underlying_persistence
        {
            private ICommit _commit;
            private DateTime _end;
            private IPipelineHook _hook1;
            private IPipelineHook _hook2;
            private DateTime _start;

            protected override void Context()
            {
                _start = DateTime.Now;
                _end = DateTime.Now;
                _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 0, null, null);

                _hook1 = A.Fake<IPipelineHook>();
                A.CallTo(() => _hook1.Select(_commit)).Returns(_commit);
                pipelineHooks.Add(_hook1);

                _hook2 = A.Fake<IPipelineHook>();
                A.CallTo(() => _hook2.Select(_commit)).Returns(_commit);
                pipelineHooks.Add(_hook2);

                A.CallTo(() => persistence.GetFromTo(Bucket.Default, _start, _end)).Returns(new List<ICommit> {_commit});
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
                A.CallTo(() => persistence.GetFromTo(Bucket.Default, _start, _end)).MustHaveHappened(Repeated.Exactly.Once);
            }

            [Fact]
            public void should_pass_all_events_through_the_pipeline_hooks()
            {
                A.CallTo(() => _hook1.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
                A.CallTo(() => _hook2.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

#if MSTEST
		[TestClass]
#endif
		public class when_committing : using_underlying_persistence
        {
            private CommitAttempt _attempt;

            protected override void Context()
            {
                _attempt = new CommitAttempt(streamId, 1, Guid.NewGuid(), 1, DateTime.Now, null, new List<EventMessage> {new EventMessage()});
            }

            protected override void Because()
            {
                Decorator.Commit(_attempt);
            }

            [Fact]
            public void should_dispose_the_underlying_persistence()
            {
                A.CallTo(() => persistence.Commit(_attempt)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

#if MSTEST
		[TestClass]
#endif
		public class when_reading_the_all_events_from_checkpoint : using_underlying_persistence
        {
            private ICommit _commit;
            private IPipelineHook _hook1;
            private IPipelineHook _hook2;

            protected override void Context()
            {
                _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, 0, null, null);

                _hook1 = A.Fake<IPipelineHook>();
                A.CallTo(() => _hook1.Select(_commit)).Returns(_commit);
                pipelineHooks.Add(_hook1);

                _hook2 = A.Fake<IPipelineHook>();
                A.CallTo(() => _hook2.Select(_commit)).Returns(_commit);
                pipelineHooks.Add(_hook2);

                A.CallTo(() => persistence.GetFrom(0)).Returns(new List<ICommit> {_commit});
            }

            protected override void Because()
            {
                Decorator.GetFrom(0).ToList();
            }

            [Fact]
            public void should_call_the_underlying_persistence_to_get_events()
            {
                A.CallTo(() => persistence.GetFrom(0)).MustHaveHappened(Repeated.Exactly.Once);
            }

            [Fact]
            public void should_pass_all_events_through_the_pipeline_hooks()
            {
                A.CallTo(() => _hook1.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
                A.CallTo(() => _hook2.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

#if MSTEST
		[TestClass]
#endif
		public class when_purging : using_underlying_persistence
        {
            private IPipelineHook _hook;

            protected override void Context()
            {
                _hook = A.Fake<IPipelineHook>();
                pipelineHooks.Add(_hook);
            }

            protected override void Because()
            {
                Decorator.Purge();
            }

            [Fact]
            public void should_call_the_pipeline_hook_purge()
            {
                A.CallTo(() => _hook.OnPurge(null)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

#if MSTEST
		[TestClass]
#endif
		public class when_purging_a_bucket : using_underlying_persistence
        {
            private IPipelineHook _hook;
            private const string _bucketId = "Bucket";

            protected override void Context()
            {
                _hook = A.Fake<IPipelineHook>();
                pipelineHooks.Add(_hook);
            }

            protected override void Because()
            {
                Decorator.Purge(_bucketId);
            }

            [Fact]
            public void should_call_the_pipeline_hook_purge()
            {
                A.CallTo(() => _hook.OnPurge(_bucketId)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

#if MSTEST
		[TestClass]
#endif
		public class when_deleting_a_stream : using_underlying_persistence
        {
            private IPipelineHook _hook;
            private const string _bucketId = "Bucket";
            private const string _streamId = "Stream";

            protected override void Context()
            {
                _hook = A.Fake<IPipelineHook>();
                pipelineHooks.Add(_hook);
            }

            protected override void Because()
            {
                Decorator.DeleteStream(_bucketId, _streamId);
            }

            [Fact]
            public void should_call_the_pipeline_hook_purge()
            {
                A.CallTo(() => _hook.OnDeleteStream(_bucketId, _streamId)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

		public abstract class using_underlying_persistence : SpecificationBase
        {
            private PipelineHooksAwarePersistanceDecorator decorator;
            protected readonly IPersistStreams persistence = A.Fake<IPersistStreams>();
            protected readonly List<IPipelineHook> pipelineHooks = new List<IPipelineHook>();
            protected readonly string streamId = Guid.NewGuid().ToString();

            public PipelineHooksAwarePersistanceDecorator Decorator
            {
                get { return decorator ?? (decorator = new PipelineHooksAwarePersistanceDecorator(persistence, pipelineHooks.Select(x => x))); }
                set { decorator = value; }
            }
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169