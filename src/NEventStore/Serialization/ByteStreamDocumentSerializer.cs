using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore.Serialization
{
    /// <summary>
    /// A document serializer that uses a serializer to serialize and deserialize objects to and from byte arrays.
    /// </summary>
    public class ByteStreamDocumentSerializer : IDocumentSerializer
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(ByteStreamDocumentSerializer));
        private readonly ISerialize _serializer;

        /// <summary>
        /// Initializes a new instance of the ByteStreamDocumentSerializer class.
        /// </summary>
        public ByteStreamDocumentSerializer(ISerialize serializer)
        {
            _serializer = serializer;
        }

        /// <inheritdoc/>
        /// <remarks>Serializes the object graph in a byte array</remarks>
        public object Serialize<T>(T graph) where T : notnull
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.SerializingGraph, typeof(T));
            }
            return _serializer.Serialize(graph);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Accepts a byte array (in the form of a byte array or a base64 encoded string)
        /// and deserialize it to an object graph.
        /// </remarks>
        public T? Deserialize<T>(object document)
        {
            var bytes = (FromBase64(document as string) ?? document as byte[])
                ?? throw new NotSupportedException("document must be byte[] or a string representing base64 encoded byte[]");

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            }
            return _serializer.Deserialize<T>(bytes);
        }

        private static byte[]? FromBase64(string? value)
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