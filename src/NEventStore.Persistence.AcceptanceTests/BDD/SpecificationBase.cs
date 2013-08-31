namespace NEventStore.Persistence.AcceptanceTests.BDD
{
    using Xunit;

    [RunWith(typeof (SpecificationBaseRunner))]
    public abstract class SpecificationBase
    {
        protected virtual void Because()
        {}

        protected virtual void Cleanup()
        {}

        protected virtual void Context()
        {}

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