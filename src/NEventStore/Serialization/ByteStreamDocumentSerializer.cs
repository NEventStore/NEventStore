using System;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore.Serialization
{
    public class ByteStreamDocumentSerializer : IDocumentSerializer
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(ByteStreamDocumentSerializer));
        private readonly ISerialize _serializer;

        public ByteStreamDocumentSerializer(ISerialize serializer)
        {
            _serializer = serializer;
        }

        public object Serialize<T>(T graph)
        {
            Logger.LogTrace(Messages.SerializingGraph, typeof(T));
            return _serializer.Serialize(graph);
        }

        public T Deserialize<T>(object document)
        {
            Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            var bytes = FromBase64(document as string) ?? document as byte[];
            return _serializer.Deserialize<T>(bytes);
        }

        private static byte[] FromBase64(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            Logger.LogTrace(Messages.InspectingTextStream);

            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}