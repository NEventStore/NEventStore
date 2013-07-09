namespace EventStore.Serialization
{
    using Logging;

    public class DocumentObjectSerializer : IDocumentSerializer
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(DocumentObjectSerializer));

		public object Serialize<T>(T graph)
		{
			Logger.Verbose(Messages.SerializingGraph, typeof(T));
			return graph;
		}
		public T Deserialize<T>(object document)
		{
			Logger.Verbose(Messages.DeserializingStream, typeof(T));
			return (T)document;
		}
	}
}