#region

using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

#endregion

namespace NEventStore.Serialization;

public class GzipSerializer : ISerialize
{
    private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(GzipSerializer));
    private readonly ISerialize _inner;

    public GzipSerializer(ISerialize inner)
    {
        _inner = inner;
    }

    public virtual void Serialize<T>(Stream output, T graph)
    {
        Logger.LogTrace(Messages.SerializingGraph, typeof(T));
        using (var compress = new DeflateStream(output, CompressionMode.Compress, true))
        {
            _inner.Serialize(compress, graph);
        }
    }

    public virtual T Deserialize<T>(Stream input)
    {
        Logger.LogTrace(Messages.DeserializingStream, typeof(T));
        using (var decompress = new DeflateStream(input, CompressionMode.Decompress, true))
        {
            return _inner.Deserialize<T>(decompress);
        }
    }
}