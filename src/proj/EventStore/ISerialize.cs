namespace EventStore
{
	/// <summary>
	/// Provides the ability to serialize and deserialize an object graph.
	/// </summary>
	public interface ISerialize
	{
		/// <summary>
		/// Serializes the object graph provided.
		/// </summary>
		/// <param name="graph">The object graph to be serialized.</param>
		/// <returns>The serialized or byte representation of the serialized object.</returns>
		byte[] Serialize(object graph);

		/// <summary>
		/// Deserializes the bytes provided.
		/// </summary>
		/// <typeparam name="T">The type of object to reconstruct.</typeparam>
		/// <param name="input">The bytes from which the object will be reconstructed.</param>
		/// <returns>The reconstructed object.</returns>
		T Deserialize<T>(byte[] input);
	}
}