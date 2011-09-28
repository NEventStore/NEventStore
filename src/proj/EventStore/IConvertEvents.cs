using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventStore
{
    /// <summary>
    /// Provides the ability to upconvert an event from one type to another.
    /// </summary>
    /// <typeparam name="TSource">The event type to convert from</typeparam>
    /// <typeparam name="TTarget">The event type to convert to</typeparam>
    public interface IConvertEvents<TSource, TTarget>
    {
        TTarget Convert(TSource sourceEvent);
    }
}
