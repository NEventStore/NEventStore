namespace NEventStore
{
    /// <summary>
    ///     Represents an optimistic concurrency conflict between multiple writers.
    /// </summary>
    public class ConcurrencyException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the ConcurrencyException class.
        /// </summary>
        public ConcurrencyException()
        { }

        /// <summary>
        ///     Initializes a new instance of the ConcurrencyException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConcurrencyException(string message)
            : base(message)
        { }

        /// <summary>
        ///     Initializes a new instance of the ConcurrencyException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The message that is the cause of the current exception.</param>
        public ConcurrencyException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}