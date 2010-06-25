namespace EventStore
{
	public interface ISerialize
	{
		byte[] Serialize(object graph);
		T Deserialize<T>(byte[] input);
	}
}