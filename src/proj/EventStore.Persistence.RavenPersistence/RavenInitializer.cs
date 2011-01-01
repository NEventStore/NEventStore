namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Transactions;
	using Raven.Client;

	public class RavenInitializer : IInitializeRaven
	{
		public virtual void Initialize(IDocumentStore store)
		{
			try
			{
				using (new TransactionScope(TransactionScopeOption.Suppress))
					TryInitialize(store);
			}
			catch (Exception e)
			{
				throw new PersistenceEngineException(e.Message, e);
			}
		}
		private static void TryInitialize(IDocumentStore store)
		{
			var generator = store.Conventions.DocumentKeyGenerator;
			store.Conventions.DocumentKeyGenerator = entity =>
				AssignIdentity(entity as StreamHead) ?? generator(entity);

			store.Initialize();
		}
		private static string AssignIdentity(StreamHead stream)
		{
			return stream == null ? null : stream.StreamId.ToString();
		}
	}
}