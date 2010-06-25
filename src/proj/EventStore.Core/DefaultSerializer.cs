namespace EventStore.Core
{
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	public class DefaultSerializer : ISerialize
	{
		private readonly IFormatter formatter = new BinaryFormatter();

		public byte[] Serialize(object graph)
		{
			using (var stream = new MemoryStream())
			{
				this.Serialize(graph);
				return stream.ToArray();
			}
		}
		public virtual void Serialize(object graph, Stream output)
		{
			this.formatter.Serialize(output, graph);
		}

		public virtual T Deserialize<T>(Stream input)
		{
			return (T)this.formatter.Deserialize(input);
		}
		public T Deserialize<T>(byte[] serialized)
		{
			using (var stream = new MemoryStream(serialized))
				return this.Deserialize<T>(stream);
		}
	}
}