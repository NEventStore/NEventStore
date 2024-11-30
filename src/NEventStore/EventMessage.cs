#region

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#endregion

namespace NEventStore;

/// <summary>
///     Represents a single element in a stream of events.
/// </summary>
[Serializable]
[DataContract]
public sealed class EventMessage
{
    /// <summary>
    ///     Initializes a new instance of the EventMessage class.
    /// </summary>
    public EventMessage()
    {
        Headers = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Gets the metadata which provides additional, unstructured information about this message.
    /// </summary>
    [DataMember]
    public Dictionary<string, object> Headers { get; set; }

    /// <summary>
    ///     Gets or sets the actual event message body.
    /// </summary>
    [DataMember]
    public object Body { get; set; }
}