namespace NEventStore.Persistence
{
    /// <summary>
    ///     Indicates the ability to build a ready-to-use persistence engine.
    /// </summary>
    /// <remarks>
    ///     Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    public interface IPersistenceFactory
    {
        /// <summary>
        ///     Builds a persistence engine.
        /// </summary>
        /// <returns>A ready-to-use persistence engine.</returns>
        IPersistStreams Build();
    }
}