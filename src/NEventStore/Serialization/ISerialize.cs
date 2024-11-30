#region

using System.IO;

#endregion

namespace NEventStore.Serialization;

/// <summary>
///     Provides the ability to serialize and deserialize an object graph.
/// </summary>
/// <remarks>
///     Instances of this class must be designed to be multi-thread safe such that they can be shared between threads.
/// </remarks>
public interface ISerialize
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
}