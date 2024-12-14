namespace NEventStore.Serialization.MsgPack
{
    using MessagePack;
    using MessagePack.Resolvers;
    using Microsoft.Extensions.Logging;
    using NEventStore.Logging;
    using System;    
    using System.IO;

    /// <summary>
    /// MsgPack serializer
    /// </summary>
    public class MsgPackSerializer : ISerialize
    {
        /// <summary>
        /// Logger instance.
        /// </summary>
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(MsgPackSerializer));

        /// <summary>
        /// Serializer options
        /// </summary>
        private readonly MessagePackSerializerOptions _options;

        /// <summary>
        /// Known types.
        /// </summary>
        private readonly Type[] _knownTypes;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options">MsgPack Options.</param>
        /// <param name="knownTypes">Know types.</param>
        public MsgPackSerializer(MessagePackSerializerOptions options = null, params Type[] knownTypes)
        {
            _options = options ?? new MessagePackSerializerOptions(TypelessContractlessStandardResolver.Instance);
            if (knownTypes?.Length == 0)
            {
                knownTypes = null;
            }

            _knownTypes = knownTypes ?? new Type[0];
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var type in _knownTypes)
                {
                    Logger.LogDebug(Messages.RegisteringKnownType, type);
                }
            }
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