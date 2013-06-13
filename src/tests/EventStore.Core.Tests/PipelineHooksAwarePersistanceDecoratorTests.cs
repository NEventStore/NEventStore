
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.Tests
{
    using EventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EventStore.Persistence;
    using Moq;

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
        private Mock<IPipelineHook> hook1;
        private Mock<IPipelineHook> hook2;
        private Commit commit;
        private DateTime date;
        

        protected override void Context()
        {
            date = DateTime.Now;
            commit = new Commit(streamId, 1, Guid.NewGuid(), 1, DateTime.Now, null, null);

            hook1 = new Mock<IPipelineHook>();
            hook1.Setup(h => h.Select(commit)).Returns(commit);
            pipelineHooks.Add(hook1);

            hook2 = new Mock<IPipelineHook>();
            hook2.Setup(h => h.Select(commit)).Returns(commit);
            pipelineHooks.Add(hook2);

            persistence.Setup(p => p.GetFrom(date)).Returns(new List<Commit> { commit });
        }

        protected override void Because()
        {
            Decorator.GetFrom(date).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            persistence.Verify(x => x.GetFrom(date), Times.Once());
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            hook1.Verify(h => h.Select(commit), Times.Once());
            hook2.Verify(h => h.Select(commit), Times.Once());
        }
    }

    public class when_reading_the_all_events_to_date : using_underlying_persistence
    {
        private Mock<IPipelineHook> hook1;
        private Mock<IPipelineHook> hook2;
        private Commit commit;

        private DateTime start;
        private DateTime end;


        protected override void Context()
        {
            start = DateTime.Now;
            end = DateTime.Now;
            commit = new Commit(streamId, 1, Guid.NewGuid(), 1, DateTime.Now, null, null);

            hook1 = new Mock<IPipelineHook>();
            hook1.Setup(h => h.Select(commit)).Returns(commit);
            pipelineHooks.Add(hook1);

            hook2 = new Mock<IPipelineHook>();
            hook2.Setup(h => h.Select(commit)).Returns(commit);
            pipelineHooks.Add(hook2);

            persistence.Setup(p => p.GetFromTo(start, end)).Returns(new List<Commit> { commit });
        }

        protected override void Because()
        {
            Decorator.GetFromTo(start, end).ToList();
        }

        [Fact]
        public void should_call_the_underlying_persistence_to_get_events()
        {
            persistence.Verify(x => x.GetFromTo(start, end), Times.Once());
        }

        [Fact]
        public void should_pass_all_events_through_the_pipeline_hooks()
        {
            hook1.Verify(h => h.Select(commit), Times.Once());
            hook2.Verify(h => h.Select(commit), Times.Once());
        }
    }

    public class when_committing : using_underlying_persistence
    {
        private Commit attempt;

        protected override void Context()
        {
            attempt = new Commit(streamId, 1, Guid.NewGuid(), 1, DateTime.Now, null, null);
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

    public abstract class using_underlying_persistence : SpecificationBase
    {
        protected Guid streamId = Guid.NewGuid();
        protected Mock<IPersistStreams> persistence = new Mock<IPersistStreams>();
        protected List<Mock<IPipelineHook>> pipelineHooks = new List<Mock<IPipelineHook>>();
        PipelineHooksAwarePersistanceDecorator decorator;

        public PipelineHooksAwarePersistanceDecorator Decorator
        {
            get { return decorator ?? (decorator = new PipelineHooksAwarePersistanceDecorator(persistence.Object, pipelineHooks.Select(x => x.Object))); }
            set { decorator = value; }
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169