namespace NEventStore
{
    /// <summary>
    /// Exception thrown when an error occurs while reading commits from a stream
    /// </summary>
    [Serializable]
    public class CommitStreamObserverException : Exception
    {
        /// <summary>
        /// The global checkpoint when the error occurred
        /// </summary>
        public long Checkpoint { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public CommitStreamObserverException() { }

        /// <summary>
        /// Constructor with a message
        /// </summary>
        public CommitStreamObserverException(string message) : base(message) { }

        /// <summary>
        /// Constructor with a message and an inner exception
        /// </summary>
        public CommitStreamObserverException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor for serialization
        /// </summary>
        protected CommitStreamObserverException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}