namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Represents a single element in a stream of events.
    /// </summary>
    [Serializable]
    [DataContract]
    public sealed class EventMessage
    {
        private Dictionary<string, object>? _headers;

        /// <summary>
        ///     Initializes a new instance of the EventMessage class.
        /// </summary>
        public EventMessage()
        {}

        /// <summary>
        ///     Gets the metadata which provides additional, unstructured information about this message.
        /// </summary>
        /// <remarks>
        /// Headers stay lazily allocated because the common case for an event message is "body only".
        /// That removes one dictionary allocation from hot paths such as stream writes while keeping
        /// the public contract intact: callers and serializers still observe a non-null, writable
        /// dictionary whenever they read the property. The setter remains available so existing
        /// serializers can populate the property during deserialization without any custom handling.
        /// </remarks>
        [DataMember]
        public Dictionary<string, object> Headers
        {
            get { return _headers ??= []; }
            set { _headers = value; }
        }

        /// <summary>
        ///     Gets or sets the actual event message body.
        /// </summary>
        [DataMember]
        public object Body { get; set; }
    }
}
