namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Transactions;
	using Raven.Client.Document;

	public class RavenInitializer
	{
		private readonly DocumentStore store;

		public RavenInitializer(DocumentStore store)
		{
			// TODO: define conventions
			this.store = store;
		}

		public void Initialize()
		{
			try
			{
				this.TryInitialize();
			}
			catch (Exception e)
			{
				throw new PersistenceException(e.Message, e);
			}
		}
		private void TryInitialize()
		{
			// TODO: create indexes
			using (new TransactionScope(TransactionScopeOption.Suppress))
				this.store.Initialize();
		}
	}
}