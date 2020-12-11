// using Microsoft.Extensions.Logging;
// using NEventStore.Logging;

namespace NEventStore.Serialization
{
    public class DocumentObjectSerializer : IDocumentSerializer
    {
        // private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(DocumentObjectSerializer));

        public object Serialize<T>(T graph)
        {
            // Logger.LogTrace(Messages.SerializingGraph, typeof(T));
            return graph;
        }

        public T Deserialize<T>(object document)
        {
            // Logger.LogTrace(Messages.DeserializingStream, typeof(T));
            return (T)document;
        }
    }
}