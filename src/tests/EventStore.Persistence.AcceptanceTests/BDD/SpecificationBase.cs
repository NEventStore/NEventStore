using Xunit;

namespace EventStore.Persistence.AcceptanceTests.BDD
{
    [RunWith(typeof(SpecificationBaseRunner))]
    public abstract class SpecificationBase
    {
        protected virtual void Because() { }

        protected virtual void Cleanup() { }

        protected virtual void Context() { }

        public void OnFinish()
        {
            Cleanup();
        }

        public void OnStart()
        {
            Context();
            Because();
        }
    }
}