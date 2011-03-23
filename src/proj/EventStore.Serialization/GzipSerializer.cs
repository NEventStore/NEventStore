namespace EventStore.Serialization
{
	using System.IO;
	using System.IO.Compression;

	public class GzipSerializer : ISerialize
	{
		private readonly ISerialize inner;

		public GzipSerializer(ISerialize inner)
		{
			this.inner = inner;
		}

		public virtual void Serialize<T>(Stream output, T graph)
		{
			using (var compress = new DeflateStream(output, CompressionMode.Compress, true))
				this.inner.Serialize(compress, graph);
		}
		public virtual T Deserialize<T>(Stream input)
		{
			using (var decompress = new DeflateStream(input, CompressionMode.Decompress, true))
				return this.inner.Deserialize<T>(decompress);
		}
	}
}