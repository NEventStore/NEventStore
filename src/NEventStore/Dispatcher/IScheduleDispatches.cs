namespace NEventStore.Dispatcher
{
    using System;

    /// <summary>
    ///     Indicates the ability to schedule the specified commit for delivery--either now or in the future.
    /// </summary>
    /// <remarks>
    ///     Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    [Obsolete("This will be removed in v6 https://github.com/NEventStore/NEventStore/issues/360", false)]
    public interface IScheduleDispatches : IDisposable
    {
        /// <summary>
        ///     Schedules the series of messages contained within the commit provided for delivery to all interested parties.
        /// </summary>
        /// <param name="commit">The commit representing the series of messages to be dispatched.</param>
        void ScheduleDispatch(ICommit commit);

        /// <summary>
        /// Start the dispatcher.
        /// </summary>
        void Start();
    }
}