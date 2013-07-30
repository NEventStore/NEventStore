namespace NEventStore.Serialization
{
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using NEventStore.Logging;

    public class BinarySerializer : ISerialize
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (BinarySerializer));
        private readonly IFormatter _formatter = new BinaryFormatter();

        public virtual void Serialize<T>(Stream output, T graph)
        {
            Logger.Verbose(Messages.SerializingGraph, typeof (T));
            _formatter.Serialize(output, graph);
        }

        public virtual T Deserialize<T>(Stream input)
        {
            Logger.Verbose(Messages.DeserializingStream, typeof (T));
            return (T) _formatter.Deserialize(input);
        }
    }
}