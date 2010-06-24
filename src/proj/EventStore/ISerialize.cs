namespace EventStore
{
	public interface ISerialize
	{
		byte[] Serialize<T>(T graph);
		T Deserialize<T>(byte[] serialized);
	}
}