
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
    using Xunit;

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

    public class when_reading_the_all_events_from_date : using_underlying_persistence
    {
        private ICommit _commit;
        private DateTime _date;
        private IPipelineHook _hook1;
        private IPipelineHook _hook2;

        protected override void Context()
        {
            _date = DateTime.Now;
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, new LongCheckpoint(0).Value, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFrom(Bucket.Default, _date)).Returns(new List<ICommit> { _commit });
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

    public class when_reading_the_all_events_to_date : using_underlying_persistence
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
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, new LongCheckpoint(0).Value, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFromTo(Bucket.Default, _start, _end)).Returns(new List<ICommit> { _commit });
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

    public class when_committing : using_underlying_persistence
    {
        private CommitAttempt _attempt;

        protected override void Context()
        {
            _attempt = new CommitAttempt(streamId, 1, Guid.NewGuid(), 1, DateTime.Now, null, new List<EventMessage>{ new EventMessage() });
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

    public class when_reading_the_all_events_from_checkpoint : using_underlying_persistence
    {
        private ICommit _commit;
        private IPipelineHook _hook1;
        private IPipelineHook _hook2;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, new LongCheckpoint(0).Value, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetFrom(null)).Returns(new List<ICommit> { _commit });
        }

        protected override void Because()
        {
            Decorator.GetFrom(null).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetFrom(null)).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => _hook2.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
        }
    }

    public class when_reading_the_all_events_get_undispatched : using_underlying_persistence
    {
        private ICommit _commit;
        private IPipelineHook _hook1;
        private IPipelineHook _hook2;

        protected override void Context()
        {
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, new LongCheckpoint(0).Value, null, null);

            _hook1 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook1.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook1);

            _hook2 = A.Fake<IPipelineHook>();
            A.CallTo(() => _hook2.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(_hook2);

            A.CallTo(() => persistence.GetUndispatchedCommits()).Returns(new List<ICommit> { _commit });
        }

        protected override void Because()
        {
            Decorator.GetUndispatchedCommits().ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            A.CallTo(() => persistence.GetUndispatchedCommits()).MustHaveHappened(Repeated.Exactly.Once);
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            A.CallTo(() => _hook1.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => _hook2.Select(_commit)).MustHaveHappened(Repeated.Exactly.Once);
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

// ReSharper enable InconsistentNaming
#pragma warning restore 169