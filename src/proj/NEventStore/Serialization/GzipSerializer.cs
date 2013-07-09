namespace NEventStore.Serialization
{
    using System.IO;
    using System.IO.Compression;
    using Logging;

    public class GzipSerializer : ISerialize
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(GzipSerializer));
		private readonly ISerialize inner;

		public GzipSerializer(ISerialize inner)
		{
			this.inner = inner;
		}

		public virtual void Serialize<T>(Stream output, T graph)
		{
			Logger.Verbose(Messages.SerializingGraph, typeof(T));
			using (var compress = new DeflateStream(output, CompressionMode.Compress, true))
				this.inner.Serialize(compress, graph);
		}
		public virtual T Deserialize<T>(Stream input)
		{
			Logger.Verbose(Messages.DeserializingStream, typeof(T));
			using (var decompress = new DeflateStream(input, CompressionMode.Decompress, true))
				return this.inner.Deserialize<T>(decompress);
		}
	}
}