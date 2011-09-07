namespace EventStore.Serialization
{
	using System;

	public class ByteStreamDocumentSerializer : IDocumentSerializer
	{
		private const string Base64Prefix = "AAEAAAD/////";
		private readonly ISerialize serializer;

		public ByteStreamDocumentSerializer(ISerialize serializer)
		{
			this.serializer = serializer;
		}

		public object Serialize<T>(T graph)
		{
			return this.serializer.Serialize(graph);
		}
		public T Deserialize<T>(object document)
		{
			var bytes = FromBase64(document as string) ?? document as byte[];
			return this.serializer.Deserialize<T>(bytes);
		}
		private static byte[] FromBase64(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			if (!value.StartsWith(Base64Prefix))
				return null;

			return Convert.FromBase64String(value);
		}
	}
}