namespace NEventStore
{
    /// <summary>
    ///    Provides support for upconverting events during the commit phase.
    /// </summary>
    public static class EventUpconverterWireupExtensions
    {
        /// <summary>
        ///   Configures the event store to upconvert events during the commit phase.
        /// </summary>
        public static EventUpconverterWireup UsingEventUpconversion(this Wireup wireup)
        {
            return new EventUpconverterWireup(wireup);
        }
    }
}