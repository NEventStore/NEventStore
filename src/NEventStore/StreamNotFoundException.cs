namespace NEventStore
{
    /// <summary>
    ///     Represents an attempt to retrieve a nonexistent event stream.
    /// </summary>
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
    }
}