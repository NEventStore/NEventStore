namespace EventStore.Serialization
{
	using System.IO;
	using System.IO.Compression;

	public class CompressedSerializer : ISerializeObjects
	{
		private readonly DefaultSerializer inner;

		public CompressedSerializer(DefaultSerializer inner)
		{
			this.inner = inner;
		}

		public virtual void Serialize(Stream output, object graph)
		{
			using (var compress = new DeflateStream(output, CompressionMode.Compress, true))
				this.inner.Serialize(compress, graph);
		}
		public virtual object Deserialize(Stream serialized)
		{
			using (var decompress = new DeflateStream(serialized, CompressionMode.Decompress, true))
				return this.inner.Deserialize(decompress);
		}
	}
}