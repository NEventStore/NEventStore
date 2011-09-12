namespace EventStore.Serialization.AcceptanceTests
{
	using System;

	public class SerializationFactory
	{
		private static readonly byte[] EncryptionKey = new byte[]
		{
			0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0x0
		};

		public virtual ISerialize Build()
		{
			switch ("serializer".GetSetting())
			{
				case "Binary":
					return new BinarySerializer();
				case "Gzip":
					return new GzipSerializer(new BinarySerializer());
				case "Rijndael":
					return new RijndaelSerializer(new BinarySerializer(), EncryptionKey);
				case "Json":
					return new JsonSerializer();
				case "Bson":
					return new BsonSerializer();
				case "ServiceStackJson":
					return new JsonSerializer();
				default:
					throw new NotSupportedException("The configured serializer is not registered with the SerializationFactory.");
			}
		}
	}
}