namespace NEventStore
{
    using System;
    using CommonDomain;
    using CommonDomain.Core;

    public static class ConventionEventRouterBuilder
    {
        public static Func<IAggregate, IRouteEvents> Build = (aggregate) => new ConventionEventRouter(true, aggregate);
    }
}