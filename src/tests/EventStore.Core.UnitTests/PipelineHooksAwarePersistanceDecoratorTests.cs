namespace EventStore.Core.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using EventStore.Persistence;
    using Machine.Specifications;
    using Moq;
    using It = Machine.Specifications.It;

    [Subject("PipelineHooksAwarePersistanceDecorator")]
    public class when_disposing_the_decorator : using_underlying_persistence
    {
        Because of = () => decorator.Dispose();

        It should_dispose_the_underlying_persistence = () => persistence.Verify(x => x.Dispose(), Times.Once());
    }

    [Subject("PipelineHooksAwarePersistanceDecorator")]
    public class when_reading_the_all_events_from_date : using_underlying_persistence
    {
        private static Mock<IPipelineHook> hook1;
        private static Mock<IPipelineHook> hook2;
        private static Commit commit;
        private static DateTime date;
        

        private Establish context = () =>
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
        };

        Because of = () => decorator.GetFrom(date).ToList();

        private It should_call_the_underlying_persistence_to_get_events = () => persistence.Verify(x => x.GetFrom(date), Times.Once());

        private It should_pass_all_events_through_the_pipeline_hooks = () =>
        {
            hook1.Verify(h => h.Select(commit), Times.Once());
            hook2.Verify(h => h.Select(commit), Times.Once());
        };
    }

    [Subject("PipelineHooksAwarePersistanceDecorator")]
    public class when_reading_the_all_events_to_date : using_underlying_persistence
    {
        private static Mock<IPipelineHook> hook1;
        private static Mock<IPipelineHook> hook2;
        private static Commit commit;

        private static DateTime start;
        private static DateTime end;


        private Establish context = () =>
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
        };

        Because of = () => decorator.GetFromTo(start, end).ToList();

        private It should_call_the_underlying_persistence_to_get_events = () => persistence.Verify(x => x.GetFromTo(start, end), Times.Once());

        private It should_pass_all_events_through_the_pipeline_hooks = () =>
        {
            hook1.Verify(h => h.Select(commit), Times.Once());
            hook2.Verify(h => h.Select(commit), Times.Once());
        };
    }

    [Subject("PipelineHooksAwarePersistanceDecorator")]
    public class when_committing : using_underlying_persistence
    {
        private static Commit attempt;

        private Establish context = () =>
        {
            attempt = new Commit(streamId, 1, Guid.NewGuid(), 1, DateTime.Now, null, null);
        };

        Because of = () => decorator.Commit(attempt);

        It should_dispose_the_underlying_persistence = () => persistence.Verify(x => x.Commit(attempt), Times.Once());
    }

    public abstract class using_underlying_persistence
    {
        protected static Guid streamId;
        protected static Mock<IPersistStreams> persistence;
        protected static List<Mock<IPipelineHook>> pipelineHooks;
        protected static PipelineHooksAwarePersistanceDecorator decorator;

        Establish context = () =>
        {
            streamId = Guid.NewGuid();
            persistence = new Mock<IPersistStreams>();
            pipelineHooks = new List<Mock<IPipelineHook>>();

            decorator = new PipelineHooksAwarePersistanceDecorator(persistence.Object, pipelineHooks.Select(x => x.Object));
        };

        private Cleanup everithing = () =>
        {
            streamId = Guid.NewGuid();
        };
    }
}
