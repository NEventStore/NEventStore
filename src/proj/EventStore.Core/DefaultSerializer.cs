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
			if (null == graph)
				return null;

			using (var stream = new MemoryStream())
			{
				this.Serialize(graph, stream);
				return stream.ToArray();
			}
		}
		public virtual void Serialize(object graph, Stream output)
		{
			this.formatter.Serialize(output, graph);
		}

		public T Deserialize<T>(byte[] serialized)
		{
			if (null == serialized || 0 == serialized.Length)
				return default(T);

			using (var stream = new MemoryStream(serialized))
				return this.Deserialize<T>(stream);
		}
		public virtual T Deserialize<T>(Stream input)
		{
			return (T)this.formatter.Deserialize(input);
		}
	}
}