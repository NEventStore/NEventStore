using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore.Serialization
{
    /// <summary>
    /// A document serializer that uses a serializer to serialize and deserialize objects.
    /// </summary>
    public class ByteStreamDocumentSerializer : IDocumentSerializer
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(ByteStreamDocumentSerializer));
        private readonly ISerialize _serializer;

        /// <summary>
        /// Initializes a new instance of the ByteStreamDocumentSerializer class.
        /// </summary>
        /// <param name="serializer"></param>
        public ByteStreamDocumentSerializer(ISerialize serializer)
        {
            _serializer = serializer;
        }

        /// <inheritdoc/>
        public object Serialize<T>(T graph)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.SerializingGraph, typeof(T));
            }
            return _serializer.Serialize(graph);
        }

        /// <inheritdoc/>
        public T Deserialize<T>(object document)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            }
            byte[] bytes = FromBase64(document as string) ?? document as byte[];
            return _serializer.Deserialize<T>(bytes);
        }

        private static byte[]? FromBase64(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.InspectingTextStream);
            }

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