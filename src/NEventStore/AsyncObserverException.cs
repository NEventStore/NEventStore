namespace NEventStore
{
    /// <summary>
    /// Exception thrown when an error occurs while reading data from a stream
    /// </summary>
    [Serializable]
    public class AsyncObserverException : Exception
    {
        /// <summary>
        /// [Optional] The global checkpoint when the error occurred
        /// </summary>
        public long Checkpoint { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AsyncObserverException() { }

        /// <summary>
        /// Constructor with a message
        /// </summary>
        public AsyncObserverException(string message) : base(message) { }

        /// <summary>
        /// Constructor with a message and an inner exception
        /// </summary>
        public AsyncObserverException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor for serialization
        /// </summary>
        protected AsyncObserverException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}