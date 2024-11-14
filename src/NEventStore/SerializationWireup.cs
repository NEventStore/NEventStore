using System;

namespace NEventStore
{
    using Microsoft.Extensions.Logging;
    using NEventStore.Logging;
    using NEventStore.Serialization;

    public class SerializationWireup : Wireup
    {
        private static readonly ILogger Logger = LogFactory.BuildLogger(typeof (SerializationWireup));

        public SerializationWireup(Wireup inner, ISerialize serializer)
            : base(inner)
        {
            Container.Register(serializer);
            Container.Register(c => new DefaultEventSerializer(c.Resolve<ISerialize>()));
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

        public SerializationWireup WithCustomEventSerialization(
            Func<ISerialize, ISerializeEvents> eventSerializerFactory)
        {
            Logger.LogDebug(Messages.ConfiguringCustomEventSerialization);

            var serializer = Container.Resolve<ISerialize>();
            var eventSerializer = eventSerializerFactory(serializer);

            Logger.LogInformation(Messages.UsingCustomEventSerialization, eventSerializer.GetType());
            Container.Register(eventSerializer);
            return this;
        }
    }
}