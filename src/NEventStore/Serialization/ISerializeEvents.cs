using System;
using System.Collections.Generic;

namespace NEventStore.Serialization
{
    public interface ISerializeEvents
    {
        /// <summary>
        ///     Deserializes the stream provided and reconstructs the corresponding object graph.
        /// </summary>
        /// <param name="input">The stream of bytes from which the object will be reconstructed.</param>
        /// <returns>The reconstructed object.</returns>
        ICollection<EventMessage> DeserializeEventMessages(byte[] input, string bucketId, string streamIdOriginal,
            int streamRevision, Guid commitId,
            int commitSequence, DateTime commitStamp, long checkpoint);
    }
}