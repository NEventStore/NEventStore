﻿using System;
using System.Collections.Generic;

namespace NEventStore.Serialization
{
    public class DefaultEventSerializer : ISerializeEvents
    {
        private readonly ISerialize _serializer;

        public DefaultEventSerializer(ISerialize serializer)
        {
            _serializer = serializer;
        }

        public ICollection<EventMessage> DeserializeEventMessages(byte[] input, string bucketId, string streamIdOriginal, int streamRevision,
            Guid commitId, int commitSequence, DateTime commitStamp, long checkpoint)
        {
            return _serializer.Deserialize<List<EventMessage>>(input);
        }
    }
}