namespace EventStore.Serialization
{
	using System;
	using Logging;

	public class ByteStreamDocumentSerializer : IDocumentSerializer
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(ByteStreamDocumentSerializer));
		private readonly ISerialize serializer;

		public ByteStreamDocumentSerializer(ISerialize serializer)
		{
			this.serializer = serializer;
		}

		public object Serialize<T>(T graph)
		{
			Logger.Verbose(Messages.SerializingGraph, typeof(T));
			return this.serializer.Serialize(graph);
		}
		public T Deserialize<T>(object document)
		{
			Logger.Verbose(Messages.DeserializingStream, typeof(T));
			var bytes = FromBase64(document as string) ?? document as byte[];
			return this.serializer.Deserialize<T>(bytes);
		}
		private static byte[] FromBase64(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			Logger.Verbose(Messages.InspectingTextStream);

			try
			{
				return Convert.FromBase64String(value);
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}