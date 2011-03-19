namespace EventStore
{
	using Serialization;

	public class SerializationWireup : Wireup
	{
		public SerializationWireup(Wireup inner, ISerialize serializer)
			: base(inner)
		{
			this.Container.Register(serializer);
		}

		public SerializationWireup Compress()
		{
			var wrapped = this.Container.Resolve<ISerialize>();
			this.Container.Register<ISerialize>(new GzipSerializer(wrapped));
			return this;
		}

		public SerializationWireup EncryptWith(byte[] encryptionKey)
		{
			var wrapped = this.Container.Resolve<ISerialize>();
			this.Container.Register<ISerialize>(new RijndaelSerializer(wrapped, encryptionKey));
			return this;
		}
	}
}