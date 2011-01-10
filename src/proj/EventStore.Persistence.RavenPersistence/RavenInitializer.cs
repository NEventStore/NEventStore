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
				GetCommitIdentity(entity as Commit)
				?? GetStreamIdentity(entity as StreamHead)
				?? generator(entity);

			store.Initialize();
		}
		private static string GetCommitIdentity(Commit commit)
		{
			return commit == null ? null : commit.Id();
		}
		private static string GetStreamIdentity(StreamHead stream)
		{
			return stream == null ? null : stream.StreamId.ToString();
		}
	}
}