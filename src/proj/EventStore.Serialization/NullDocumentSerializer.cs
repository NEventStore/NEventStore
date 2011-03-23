namespace EventStore.Serialization
{
	public class NullDocumentSerializer : IDocumentSerializer
	{
		public object Serialize<T>(T graph)
		{
			return graph;
		}
		public T Deserialize<T>(object document)
		{
			return (T)document;
		}
	}
}