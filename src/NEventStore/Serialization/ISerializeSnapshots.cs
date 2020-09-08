namespace NEventStore.Serialization
{
    using System.IO;

    public interface ISerializeSnapshots
    {
        /// <summary>
        ///     Serializes the object graph provided and writes a serialized representation to the output stream provided.
        /// </summary>
        /// <typeparam name="T">The type of object to be serialized</typeparam>
        /// <param name="output">The stream into which the serialized object graph should be written.</param>
        /// <param name="graph">The object graph to be serialized.</param>
        void Serialize<T>(Stream output, T graph);

        /// <summary>
        ///     Deserializes the stream provided and reconstructs the corresponding object graph.
        /// </summary>
        /// <typeparam name="T">The type of object to be deserialized.</typeparam>
        /// <param name="input">The stream of bytes from which the object will be reconstructed.</param>
        /// <returns>The reconstructed object.</returns>
        T Deserialize<T>(Stream input);

        /// <summary>
        ///     Deserializes the array of bytes provided.
        /// </summary>
        /// <typeparam name="T">The type of object to be deserialized.</typeparam>
        /// <param name="serialized">The serialized array of bytes.</param>
        /// <returns>The reconstituted object, if any.</returns>
        T Deserialize<T>(byte[] serialized);

        /// <summary>
        ///     Serializes the object provided.
        /// </summary>
        /// <typeparam name="T">The type of object to be serialized</typeparam>
        /// <param name="value">The object graph to be serialized.</param>
        /// <returns>A serialized representation of the object graph provided.</returns>
        byte[] Serialize<T>(T value);
    }
}
