namespace NEventStore.Persistence
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Represents a general failure of the storage engine or persistence infrastructure.
    /// </summary>
    [Serializable]
    public class StorageException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the StorageException class.
        /// </summary>
        public StorageException()
        {}

        /// <summary>
        ///     Initializes a new instance of the StorageException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public StorageException(string message)
            : base(message)
        {}

        /// <summary>
        ///     Initializes a new instance of the StorageException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The message that is the cause of the current exception.</param>
        public StorageException(string message, Exception innerException)
            : base(message, innerException)
        {}

        /// <summary>
        ///     Initializes a new instance of the StorageException class.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected StorageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {}
    }
}