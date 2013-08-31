namespace NEventStore.Serialization
{
    /// <summary>
    ///     Provides the ability to serialize an object graph to and from a document.
    /// </summary>
    /// <remarks>
    ///     Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
    /// </remarks>
    public interface IDocumentSerializer
    {
        /// <summary>
        ///     Serializes the object graph provided into a document.
        /// </summary>
        /// <typeparam name="T">The type of object to be serialized</typeparam>
        /// <param name="graph">The object graph to be serialized.</param>
        /// <returns>The document form of the graph provided.</returns>
        object Serialize<T>(T graph);

        /// <summary>
        ///     Deserializes the document provided into an object graph.
        /// </summary>
        /// <typeparam name="T">The type of object graph.</typeparam>
        /// <param name="document">The document to be deserialized.</param>
        /// <returns>An object graph of the specified type.</returns>
        T Deserialize<T>(object document);
    }
}