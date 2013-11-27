namespace NEventStore
{
    /// <summary>
    /// Dispatcher startup types.
    /// </summary>
    public enum DispatcherSchedulerStartup
    {
        /// <summary>
        /// Dispatcher will be started automatically.
        /// </summary>
        Auto,
        /// <summary>
        /// Dispather will be started explicitly.
        /// </summary>
        Explicit
    }
}