namespace EventStore.Serialization.AcceptanceTests
{
	public class SerializationFactory
	{
		public ISerialize Build()
		{
			return new BinarySerializer();
		}
	}
}