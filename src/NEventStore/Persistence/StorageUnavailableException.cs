namespace NEventStore.Persistence
{
    /// <summary>
    ///     Indicates that the underlying persistence medium is unavailable or offline.
    /// </summary>
    public class StorageUnavailableException : StorageException
    {
        /// <summary>
        ///     Initializes a new instance of the StorageUnavailableException class.
        /// </summary>
        public StorageUnavailableException()
        {}

        /// <summary>
        ///     Initializes a new instance of the StorageUnavailableException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public StorageUnavailableException(string message)
            : base(message)
        {}

        /// <summary>
        ///     Initializes a new instance of the StorageUnavailableException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The message that is the cause of the current exception.</param>
        public StorageUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {}
    }
}