namespace NEventStore
{
    using System.Collections.Generic;

    public interface IEventMessage
    {
        /// <summary>
        ///     Gets the metadata which provides additional, unstructured information about this message.
        /// </summary>
        IDictionary<string, object> Headers { get; }

        /// <summary>
        ///     Gets or sets the actual event message body.
        /// </summary>
        object Body { get; }
    }
}