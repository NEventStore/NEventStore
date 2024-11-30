#region

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;

#endregion

namespace NEventStore.Serialization;

/// <summary>
///     Delegates to <see cref="BinaryFormatter" /> to perform the actual serialization.
/// </summary>
public sealed class BinarySerializer : ISerialize
{
    private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(BinarySerializer));

    private static readonly MessagePackSerializerOptions _options = new(TypelessContractlessStandardResolver.Instance);

    public void Serialize<T>(Stream output, T graph)
    {
        Logger.LogTrace(Messages.SerializingGraph, typeof(T));
        MessagePackSerializer.Serialize(output, graph, _options);
    }

    public T Deserialize<T>(Stream input)
    {
        Logger.LogTrace(Messages.DeserializingStream, typeof(T));
        return MessagePackSerializer.Deserialize<T>(input, _options);
    }
}