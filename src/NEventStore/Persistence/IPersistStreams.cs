namespace NEventStore.Persistence
{
    /// <summary>
    ///     Indicates the ability to adapt the underlying persistence infrastructure to behave like a stream of events.
    /// </summary>
    /// <remarks>
    ///     Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    public interface IPersistStreams : IDisposable
        , IPersistStreamsSync
        , IPersistStreamsAsync
    {
        /// <summary>
        ///     Gets a value indicating whether this instance has been disposed of.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        ///     Initializes and prepares the storage for use, if not already performed.
        /// </summary>
        /// <remarks>
        /// Store initialization will be synchronous and should be completed before the method returns.
        /// This is to ensure that the storage is ready for use before the built storage instance is registered in a container.
        /// Another option will be: remove the Wireup.InitializeStorageEngine() method and let the user call Initialize() or InitializeAsync() explicitly.
        /// </remarks>
        /// <exception cref="StorageException" />
        /// <exception cref="StorageUnavailableException" />
        void Initialize();

        /// <summary>
        ///     Completely DESTROYS the contents and schema (if applicable) containing ANY and ALL streams that have been
        ///     successfully persisted.
        ///     Use with caution.
        /// </summary>
        void Drop();
    }
}