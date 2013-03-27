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
    public class when_reading_the_stream : using_underlying_persistence
    {
        Because of = () => decorator.GetFrom(streamId, 1, 2);

        It should_call_the_underlying_persistence = () => persistence.Verify(x => x.GetFrom(streamId, 1, 2), Times.Once());
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
