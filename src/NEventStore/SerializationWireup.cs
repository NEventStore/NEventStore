namespace NEventStore
{
    using NEventStore.Logging;
    using NEventStore.Serialization;

    public class SerializationWireup : Wireup
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (SerializationWireup));

        public SerializationWireup(Wireup inner, ISerialize serializer)
            : base(inner)
        {
            Container.Register(serializer);
        }

        public SerializationWireup Compress()
        {
            Logger.Debug(Messages.ConfiguringCompression);
            var wrapped = Container.Resolve<ISerialize>();

            Logger.Info(Messages.WrappingSerializerGZip, wrapped.GetType());
            Container.Register<ISerialize>(new GzipSerializer(wrapped));
            return this;
        }

#if !NETSTANDARD1_6
        public SerializationWireup EncryptWith(byte[] encryptionKey)
        {
            Logger.Debug(Messages.ConfiguringEncryption);
            var wrapped = Container.Resolve<ISerialize>();

            Logger.Info(Messages.WrappingSerializerEncryption, wrapped.GetType());
            Container.Register<ISerialize>(new RijndaelSerializer(wrapped, encryptionKey));
            return this;
        }
#endif
    }
}