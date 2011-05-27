namespace EventStore
{
	using Persistence.MongoPersistence;
	using Serialization;

	public class MongoPersistenceWireup : PersistenceWireup
	{
		public MongoPersistenceWireup(Wireup inner, string connectionName, IDocumentSerializer serializer)
			: base(inner)
		{
			this.Container.Register(c => new MongoPersistenceFactory(
				connectionName,
				serializer).Build());
		}
	}
}