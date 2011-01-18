namespace EventStore.Serialization.AcceptanceTests
{
	using System;

	public class SerializationFactory
	{
		public ISerialize Build()
		{
			switch ("serializer".GetSetting())
			{
				case "Binary":
					return new BinarySerializer();
				case "Compressed":
					return new CompressedSerializer(new BinarySerializer());
				case "Xml":
					return new XmlSerializer(typeof(SimpleMessage));
				case "Json":
					return new JsonSerializer();
				case "Bson":
					return new BsonSerializer();
				case "ProtocolBuffers":
					return new ProtocolBufferSerializer();
				default:
					throw new NotSupportedException("The configured serializer is not registered with the SerializationFactory.");
			}
		}
	}
}