namespace EventStore
{
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	public class DefaultSerializer : ISerialize
	{
		private readonly IFormatter formatter = new BinaryFormatter();

		public byte[] Serialize<T>(T graph)
		{
			using (var stream = new MemoryStream())
			{
				this.formatter.Serialize(stream, graph);
				return stream.ToArray();
			}
		}
		public T Deserialize<T>(byte[] serialized)
		{
			using (var stream = new MemoryStream(serialized))
				return (T)this.formatter.Deserialize(stream);
		}
	}
}