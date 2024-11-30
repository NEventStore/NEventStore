namespace NEventStore.Tests;

public class TestableWireup : Wireup
{
    public TestableWireup(Wireup inner) : base(inner)
    {
    }

    public new NanoContainer Container => base.Container;
}