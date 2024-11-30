using Microsoft.Extensions.Logging;
using NEventStore.Logging;
using NEventStore.Serialization;

namespace NEventStore
{
    public class SerializationWireup : Wireup
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof(SerializationWireup));

        public SerializationWireup(Wireup inner, ISerialize serializer)
            : base(inner)
        {
            Container.Register(serializer);
        }

        public SerializationWireup Compress()
        {
            Logger.LogDebug(Messages.ConfiguringCompression);
            var wrapped = Container.Resolve<ISerialize>();

            Logger.LogInformation(Messages.WrappingSerializerGZip, wrapped.GetType());
            Container.Register<ISerialize>(new GzipSerializer(wrapped));
            return this;
        }

        public SerializationWireup EncryptWith(byte[] encryptionKey)
        {
            Logger.LogDebug(Messages.ConfiguringEncryption);
            var wrapped = Container.Resolve<ISerialize>();

            Logger.LogInformation(Messages.WrappingSerializerEncryption, wrapped.GetType());
            Container.Register<ISerialize>(new RijndaelSerializer(wrapped, encryptionKey));
            return this;
        }
    }
}