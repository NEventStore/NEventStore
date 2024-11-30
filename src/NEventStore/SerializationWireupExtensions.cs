#region

using System;
using Microsoft.Extensions.Logging;
using NEventStore.Logging;
using NEventStore.Serialization;

#endregion

namespace NEventStore;

public static class SerializationWireupExtensions
{
    private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(PersistenceWireup));

    [Obsolete(
        "BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.")]
    public static SerializationWireup UsingBinarySerialization(this PersistenceWireup wireup)
    {
        Logger.LogInformation(Resources.WireupSetSerializer, "Binary");
        return new SerializationWireup(wireup, new BinarySerializer());
    }

    public static SerializationWireup UsingCustomSerialization(this PersistenceWireup wireup, ISerialize serializer)
    {
        Logger.LogInformation(Resources.WireupSetSerializer, "Custom" + serializer.GetType());
        return new SerializationWireup(wireup, serializer);
    }
}