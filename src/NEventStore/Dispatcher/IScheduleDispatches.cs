namespace NEventStore.Dispatcher
{
    using System;

    /// <summary>
    ///     Indicates the ability to schedule the specified commit for delivery--either now or in the future.
    /// </summary>
    /// <remarks>
    ///     Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    public interface IScheduleDispatches : IDisposable
    {
        /// <summary>
        ///     Schedules the series of messages contained within the commit provided for delivery to all interested parties.
        /// </summary>
        /// <param name="commit">The commit representing the series of messages to be dispatched.</param>
        void ScheduleDispatch(ICommit commit);
    }
}