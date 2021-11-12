namespace NEventStore.Serialization
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using Microsoft.Extensions.Logging;
    using NEventStore.Logging;

    /// <summary>
    /// Delegates to <see cref="BinaryFormatter"/> to perform the actual serialization.
    /// </summary>
    [Obsolete("BinaryFormatter serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for more information.")]
    public class BinarySerializer : ISerialize
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof (BinarySerializer));
        private readonly IFormatter _formatter = new BinaryFormatter();

        public virtual void Serialize<T>(Stream output, T graph)
        {
            Logger.LogTrace(Messages.SerializingGraph, typeof (T));
            _formatter.Serialize(output, graph);
        }

        public virtual T Deserialize<T>(Stream input)
        {
            Logger.LogTrace(Messages.DeserializingStream, typeof (T));
            return (T) _formatter.Deserialize(input);
        }
    }
}