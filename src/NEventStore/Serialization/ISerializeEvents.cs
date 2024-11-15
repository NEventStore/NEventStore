using System;
using System.Collections.Generic;

namespace NEventStore.Serialization
{
    public interface ISerializeEvents
    {
        /// <summary>
        ///     Deserializes the stream provided and reconstructs the corresponding object graph.
        /// </summary>
        /// <param name="input">The bytes from which the object will be reconstructed.</param>
        /// <param name="bucketId">The <see cref="ICommit.BucketId"/>.</param>
        /// <param name="streamId">The <see cref="ICommit.StreamId"/>.</param>
        /// <param name="streamRevision">The <see cref="ICommit.StreamRevision"/>.</param>
        /// <param name="commitId">The <see cref="ICommit.CommitId"/>.</param>
        /// <param name="commitSequence">The <see cref="ICommit.CommitSequence"/>.</param>
        /// <param name="commitStamp">The <see cref="ICommit.CommitStamp"/>.</param>
        /// <param name="checkpoint">The <see cref="ICommit.CheckpointToken"/>.</param>
        /// <param name="headers">The <see cref="ICommit.Headers"/>.</param>
        /// <returns>The reconstructed event messages.</returns>
        ICollection<EventMessage> DeserializeEventMessages(byte[] input, string bucketId, string streamId,
            int streamRevision, Guid commitId,
            int commitSequence, DateTime commitStamp, long checkpoint, IReadOnlyDictionary<string, object> headers);
    }
}