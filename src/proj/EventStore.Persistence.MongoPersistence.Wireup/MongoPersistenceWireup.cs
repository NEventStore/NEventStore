namespace EventStore
{
	using Logging;
	using Persistence.MongoPersistence;
	using Serialization;

	public class MongoPersistenceWireup : PersistenceWireup
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(MongoPersistenceWireup));

		public MongoPersistenceWireup(Wireup inner, string connectionName, IDocumentSerializer serializer)
			: base(inner)
		{
			Logger.Debug("Configuring Mongo persistence engine.");

			this.Container.Register(c => new MongoPersistenceFactory(
				connectionName,
				serializer).Build());
		}
	}
}