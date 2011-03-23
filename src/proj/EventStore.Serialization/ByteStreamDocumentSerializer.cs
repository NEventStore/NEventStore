namespace EventStore.Serialization
{
	public class ByteStreamDocumentSerializer : IDocumentSerializer
	{
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
			return this.serializer.Deserialize<T>(document as byte[]);
		}
	}
}