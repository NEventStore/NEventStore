using Microsoft.Extensions.Logging;
using NEventStore.Logging;
using NEventStore.Serialization;

namespace NEventStore
{
    /// <summary>
    ///    Represents the configuration for serialization.
    /// </summary>
    public class SerializationWireup : Wireup
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(SerializationWireup));

        /// <summary>
        ///    Initializes a new instance of the SerializationWireup class.
        /// </summary>
        public SerializationWireup(Wireup inner, ISerialize serializer)
            : base(inner)
        {
            Container.Register(serializer);
        }

        /// <summary>
        /// Enable GZip compression on the serialized stream.
        /// </summary>
        public SerializationWireup Compress()
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Messages.ConfiguringCompression);
            }
            var wrapped = Container.Resolve<ISerialize>();

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Messages.WrappingSerializerGZip, wrapped.GetType());
            }
            Container.Register<ISerialize>(new GzipSerializer(wrapped));
            return this;
        }

        /// <summary>
        /// Enable Rijndael encryption on the serialized stream.
        /// </summary>
        public SerializationWireup EncryptWith(byte[] encryptionKey)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(Messages.ConfiguringEncryption);
            }
            var wrapped = Container.Resolve<ISerialize>();

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(Messages.WrappingSerializerEncryption, wrapped.GetType());
            }
            Container.Register<ISerialize>(new RijndaelSerializer(wrapped, encryptionKey));
            return this;
        }
    }
}