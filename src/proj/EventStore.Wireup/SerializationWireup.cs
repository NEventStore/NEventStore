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

		public SerializationWireup Compressed()
		{
			var wrapped = this.Container.Resolve<ISerialize>();
			this.Container.Register(new GzipSerializer(wrapped));
			return this;
		}

		public SerializationWireup Encrypted(byte[] encryptionKey)
		{
			var wrapped = this.Container.Resolve<ISerialize>();
			this.Container.Register(new RijndaelSerializer(wrapped, encryptionKey));
			return this;
		}
	}
}