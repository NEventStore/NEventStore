namespace NEventStore
{
    using Logging;
    using Serialization;

    public class SerializationWireup : Wireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(SerializationWireup));

		public SerializationWireup(Wireup inner, ISerialize serializer)
			: base(inner)
		{
			this.Container.Register(serializer);
		}

		public SerializationWireup Compress()
		{
			Logger.Debug(Messages.ConfiguringCompression);
			var wrapped = this.Container.Resolve<ISerialize>();

			Logger.Debug(Messages.WrappingSerializerGZip, wrapped.GetType());
			this.Container.Register<ISerialize>(new GzipSerializer(wrapped));
			return this;
		}

		public SerializationWireup EncryptWith(byte[] encryptionKey)
		{
			Logger.Debug(Messages.ConfiguringEncryption);
			var wrapped = this.Container.Resolve<ISerialize>();

			Logger.Debug(Messages.WrappingSerializerEncryption, wrapped.GetType());
			this.Container.Register<ISerialize>(new RijndaelSerializer(wrapped, encryptionKey));
			return this;
		}
	}
}