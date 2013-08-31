namespace NEventStore
{
    public static class EventUpconverterWireupExtensions
    {
        public static EventUpconverterWireup UsingEventUpconversion(this Wireup wireup)
        {
            return new EventUpconverterWireup(wireup);
        }
    }
}