namespace NEventStore
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Represents an attempt to retrieve a nonexistent event stream.
    /// </summary>
    [Serializable]
    public class StreamNotFoundException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the StreamNotFoundException class.
        /// </summary>
        public StreamNotFoundException()
        {}

        /// <summary>
        ///     Initializes a new instance of the StreamNotFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public StreamNotFoundException(string message)
            : base(message)
        {}

        /// <summary>
        ///     Initializes a new instance of the StreamNotFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The message that is the cause of the current exception.</param>
        public StreamNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {}

#if !NETSTANDARD1_6
        /// <summary>
        ///     Initializes a new instance of the StreamNotFoundException class.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected StreamNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {}
#endif
    }
}