namespace EventStore.Core
{
	using System.IO;
	using System.IO.Compression;

	public class CompressedSerializer : DefaultSerializer
	{
		private readonly DefaultSerializer inner;

		public CompressedSerializer(DefaultSerializer inner)
		{
			this.inner = inner;
		}

		public override void Serialize(object graph, Stream output)
		{
			using (var compress = new DeflateStream(output, CompressionMode.Compress, true))
				this.inner.Serialize(graph, compress);
		}

		public override T Deserialize<T>(Stream input)
		{
			using (var decompress = new DeflateStream(input, CompressionMode.Decompress, true))
				return this.inner.Deserialize<T>(decompress);
		}
	}
}