namespace EventStore.Serialization
{
	using System.IO;
	using ServiceStack.Text;

	public class ServiceStackJsonSerializer : ISerialize
	{
		public void Serialize<T>(Stream output, T graph)
		{
			JsonSerializer.SerializeToStream(graph, output);
		}
		public T Deserialize<T>(Stream input)
		{
			return JsonSerializer.DeserializeFromStream<T>(input);
		}
	}
}