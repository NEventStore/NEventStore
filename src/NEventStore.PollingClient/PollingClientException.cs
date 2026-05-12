namespace NEventStore.PollingClient
{
    /// <summary>
    /// ApplicationException has been deprecated in .NET Core
    /// </summary>
    public class PollingClientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PollingClientException"/> class.
        /// </summary>
        public PollingClientException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="PollingClientException"/> class.
        /// </summary>
        public PollingClientException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="PollingClientException"/> class.
        /// </summary>
        public PollingClientException(string message, Exception inner) : base(message, inner) { }
    }
}
