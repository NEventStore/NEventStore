namespace NEventStore.Conversion
{
    /// <summary>
    ///     Represents the failure that occurs when there are two or more event converters created for the same source type.
    /// </summary>
    public class MultipleConvertersFoundException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the MultipleConvertersFoundException class.
        /// </summary>
        public MultipleConvertersFoundException()
        { }

        /// <summary>
        ///     Initializes a new instance of the MultipleConvertersFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MultipleConvertersFoundException(string message)
            : base(message)
        { }

        /// <summary>
        ///     Initializes a new instance of the MultipleConvertersFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MultipleConvertersFoundException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}