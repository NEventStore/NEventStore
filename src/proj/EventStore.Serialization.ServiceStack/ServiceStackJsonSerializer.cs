namespace EventStore.Serialization
{
	using System.IO;
	using Logging;
	using ServiceStack.Text;

	public class ServiceStackJsonSerializer : ISerialize
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(ServiceStackJsonSerializer));

		public void Serialize<T>(Stream output, T graph)
		{
			Logger.Verbose(Messages.SerializingGraph, typeof(T));
			JsonSerializer.SerializeToStream(graph, output);
		}
		public T Deserialize<T>(Stream input)
		{
			Logger.Verbose(Messages.DeserializingStream, typeof(T));
			return JsonSerializer.DeserializeFromStream<T>(input);
		}
	}
}