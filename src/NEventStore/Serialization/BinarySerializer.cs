namespace NEventStore.Serialization
{
#if !NETSTANDARD1_6
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using Microsoft.Extensions.Logging;
    using NEventStore.Logging;

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
#endif
}