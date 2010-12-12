namespace EventStore.Serialization
{
	using System.IO;

	/// <summary>
	/// Provides the ability to serialize and deserialize an object graph.
	/// </summary>
	public interface ISerializeObjects
	{
		/// <summary>
		/// Serializes the object graph provided.
		/// </summary>
		/// <param name="output">The stream into which the serialized object graph should be written.</param>
		/// <param name="graph">The object graph to be serialized.</param>
		void Serialize(Stream output, object graph);

		/// <summary>
		/// Deserializes the stream provided.
		/// </summary>
		/// <param name="serialized">The stream of bytes from which the object will be reconstructed.</param>
		/// <returns>The reconstructed object.</returns>
		object Deserialize(Stream serialized);
	}
}