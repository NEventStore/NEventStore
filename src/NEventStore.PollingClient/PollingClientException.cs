using System;

namespace NEventStore.PollingClient
{
    /// <summary>
    /// ApplicationException has been deprecated in .NET Core
    /// </summary>
    [Serializable]
    public class PollingClientException : Exception
    {
        public PollingClientException() { }
        public PollingClientException(string message) : base(message) { }
        public PollingClientException(string message, Exception inner) : base(message, inner) { }
#if !NETSTANDARD1_6
        protected PollingClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
