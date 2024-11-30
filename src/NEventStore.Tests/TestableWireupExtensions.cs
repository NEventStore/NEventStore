namespace NEventStore.Tests;

public static class TestableWireupExtensions
{
    public static TestableWireup UseTestableWireup(this Wireup wireup)
    {
        return new TestableWireup(wireup);
    }
}