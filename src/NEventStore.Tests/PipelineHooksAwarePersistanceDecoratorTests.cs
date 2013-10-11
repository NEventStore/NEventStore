
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Moq;
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
            persistence.Verify(x => x.Dispose(), Times.Once());
        }
    }

    public class when_reading_the_all_events_from_date : using_underlying_persistence
    {
        private ICommit _commit;
        private DateTime date;
        private Mock<IPipelineHook> hook1;
        private Mock<IPipelineHook> hook2;

        protected override void Context()
        {
            date = DateTime.Now;
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, new LongCheckpoint(0).Value, null, null);

            hook1 = new Mock<IPipelineHook>();
            hook1.Setup(h => h.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(hook1);

            hook2 = new Mock<IPipelineHook>();
            hook2.Setup(h => h.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(hook2);

            persistence.Setup(p => p.GetFrom(Bucket.Default, date)).Returns(new List<ICommit> { _commit });
        }

        protected override void Because()
        {
            Decorator.GetFrom(date).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            persistence.Verify(x => x.GetFrom(Bucket.Default, date), Times.Once());
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            hook1.Verify(h => h.Select(_commit), Times.Once());
            hook2.Verify(h => h.Select(_commit), Times.Once());
        }
    }

    public class when_reading_the_all_events_to_date : using_underlying_persistence
    {
        private ICommit _commit;
        private DateTime end;
        private Mock<IPipelineHook> hook1;
        private Mock<IPipelineHook> hook2;

        private DateTime start;

        protected override void Context()
        {
            start = DateTime.Now;
            end = DateTime.Now;
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, new LongCheckpoint(0).Value, null, null);

            hook1 = new Mock<IPipelineHook>();
            hook1.Setup(h => h.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(hook1);

            hook2 = new Mock<IPipelineHook>();
            hook2.Setup(h => h.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(hook2);

            persistence.Setup(p => p.GetFromTo(Bucket.Default, start, end)).Returns(new List<ICommit> { _commit });
        }

        protected override void Because()
        {
            Decorator.GetFromTo(start, end).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            persistence.Verify(x => x.GetFromTo(Bucket.Default, start, end), Times.Once());
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            hook1.Verify(h => h.Select(_commit), Times.Once());
            hook2.Verify(h => h.Select(_commit), Times.Once());
        }
    }

    public class when_committing : using_underlying_persistence
    {
        private CommitAttempt attempt;

        protected override void Context()
        {
            attempt = new CommitAttempt(streamId, 1, Guid.NewGuid(), 1, DateTime.Now, null, new List<EventMessage>{ new EventMessage() });
        }

        protected override void Because()
        {
            Decorator.Commit(attempt);
        }

        [Fact]
        public void should_dispose_the_underlying_persistence()
        {
            persistence.Verify(x => x.Commit(attempt), Times.Once());
        }
    }

    public class when_reading_the_all_events_from_checkpoint : using_underlying_persistence
    {
        private ICommit _commit;
        private DateTime date;
        private Mock<IPipelineHook> hook1;
        private Mock<IPipelineHook> hook2;

        protected override void Context()
        {
            date = DateTime.Now;
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, new LongCheckpoint(0).Value, null, null);

            hook1 = new Mock<IPipelineHook>();
            hook1.Setup(h => h.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(hook1);

            hook2 = new Mock<IPipelineHook>();
            hook2.Setup(h => h.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(hook2);

            persistence.Setup(p => p.GetFrom(null)).Returns(new List<ICommit> { _commit });
        }

        protected override void Because()
        {
            Decorator.GetFrom(null).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            persistence.Verify(x => x.GetFrom(null), Times.Once());
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            hook1.Verify(h => h.Select(_commit), Times.Once());
            hook2.Verify(h => h.Select(_commit), Times.Once());
        }
    }

    public class when_reading_the_all_events_get_undispatched : using_underlying_persistence
    {
        private ICommit _commit;
        private DateTime date;
        private Mock<IPipelineHook> hook1;
        private Mock<IPipelineHook> hook2;

        protected override void Context()
        {
            date = DateTime.Now;
            _commit = new Commit(Bucket.Default, streamId, 1, Guid.NewGuid(), 1, DateTime.Now, new LongCheckpoint(0).Value, null, null);

            hook1 = new Mock<IPipelineHook>();
            hook1.Setup(h => h.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(hook1);

            hook2 = new Mock<IPipelineHook>();
            hook2.Setup(h => h.Select(_commit)).Returns(_commit);
            pipelineHooks.Add(hook2);

            persistence.Setup(p => p.GetUndispatchedCommits()).Returns(new List<ICommit> { _commit });
        }

        protected override void Because()
        {
            Decorator.GetUndispatchedCommits().ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            persistence.Verify(x => x.GetUndispatchedCommits(), Times.Once());
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            hook1.Verify(h => h.Select(_commit), Times.Once());
            hook2.Verify(h => h.Select(_commit), Times.Once());
        }
    }

    public abstract class using_underlying_persistence : SpecificationBase
    {
        private PipelineHooksAwarePersistanceDecorator decorator;
        protected readonly Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
        protected readonly List<Mock<IPipelineHook>> pipelineHooks = new List<Mock<IPipelineHook>>();
        protected string streamId = Guid.NewGuid().ToString();

        public PipelineHooksAwarePersistanceDecorator Decorator
        {
            get { return decorator ?? (decorator = new PipelineHooksAwarePersistanceDecorator(persistence.Object, pipelineHooks.Select(x => x.Object))); }
            set { decorator = value; }
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169