﻿namespace NEventStore.Conversion;

/// <summary>
///     Provides the ability to upconvert an event from one type to another.
/// </summary>
/// <typeparam name="TSource">The source event type from which to convert.</typeparam>
/// <typeparam name="TTarget">The target event type.</typeparam>
public interface IUpconvertEvents<TSource, TTarget>
    where TSource : class
    where TTarget : class
{
    /// <summary>
    ///     Converts an event from one type to another.
    /// </summary>
    /// <param name="sourceEvent">The event to be converted.</param>
    /// <returns>The converted event.</returns>
    TTarget Convert(TSource sourceEvent);
}