using MessagePack;
using MessagePack.Resolvers;

namespace NEventStore.Serialization.MsgPack
{
    /// <summary>
    /// MsgPack serializer
    /// </summary>
    public class MsgPackSerializer : ISerialize
    {
        /// <summary>
        /// Serializer options
        /// </summary>
        private readonly MessagePackSerializerOptions _options;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options">MsgPack Options.</param>
        public MsgPackSerializer(MessagePackSerializerOptions? options = null)
        {
            _options = options ?? new MessagePackSerializerOptions(TypelessContractlessStandardResolver.Instance);
        }

        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="output">Output stream</param>
        /// <param name="graph">Object to deserialize.</param>
        public virtual void Serialize<T>(Stream output, T graph) => MessagePackSerializer.Serialize(output, graph, _options);

        /// <summary>
        /// Deserializes an object from stream.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="input">Stream input</param>
        /// <returns>Deserialized object</returns>
        public virtual T Deserialize<T>(Stream input) => MessagePackSerializer.Deserialize<T>(input, _options);
    }
}