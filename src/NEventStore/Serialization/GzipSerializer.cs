using System.IO.Compression;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

namespace NEventStore.Serialization
{
    /// <summary>
    ///    Represents a serializer that compresses the serialized object using GZip.
    /// </summary>
    public class GzipSerializer : ISerialize
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(GzipSerializer));
        private readonly ISerialize _inner;

        /// <summary>
        /// Initializes a new instance of the GzipSerializer class.
        /// </summary>
        public GzipSerializer(ISerialize inner)
        {
            _inner = inner;
        }

        /// <inheritdoc/>
        public virtual void Serialize<T>(Stream output, T graph)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.SerializingGraph, typeof(T));
            }
            using var compress = new DeflateStream(output, CompressionMode.Compress, true);
            _inner.Serialize(compress, graph);
        }

        /// <inheritdoc/>
        public virtual T Deserialize<T>(Stream input)
        {
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            }
            using var decompress = new DeflateStream(input, CompressionMode.Decompress, true);
            return _inner.Deserialize<T>(decompress);
        }
    }
}