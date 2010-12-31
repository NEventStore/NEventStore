namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Transactions;
	using Persistence;
	using Raven.Client;

	public class RavenPersistenceEngine : IPersistStreams
	{
		private const string ToDispatch = "ToDispatch";
		private readonly IDocumentStore store;
		private readonly IInitializeRaven initializer;
		private bool disposed;

		public RavenPersistenceEngine(IDocumentStore store, IInitializeRaven initializer)
		{
			this.store = store;
			this.initializer = initializer;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this.disposed)
				return;

			this.disposed = true;
			this.store.Dispose();
		}

		public virtual void Initialize()
		{
			this.initializer.Initialize(this.store);
		}

		public virtual IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			{
				return null;
			}
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				try
				{
					var commits = session.Query<RavenCommit>()
						.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision)
						.ToArray();

					return commits.Select(x => x.ToCommit());
				}
				catch (Exception e)
				{
					throw new PersistenceEngineException(e.Message, e);
				}
			}
		}
		public virtual void Persist(CommitAttempt uncommitted)
		{
			var commit = uncommitted.ToRavenCommit();

			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				session.Advanced.UseOptimisticConcurrency = true;
				session.Advanced.OnEntityConverted += (instance, doc, metadata) => doc.Add(ToDispatch, true);

				session.Store(commit);

				try
				{
					session.SaveChanges();
				}
				catch (Raven.Http.Exceptions.ConcurrencyException e)
				{
					var committed = session.Load<RavenCommit>(commit.Id);
					if (committed.CommitId == commit.CommitId)
						throw new DuplicateCommitException();

					throw new ConcurrencyException(e.Message, e);
				}
				catch (Exception e)
				{
					throw new PersistenceEngineException(e.Message, e);
				}
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				try
				{
					var commits = session.Advanced.LuceneQuery<RavenCommit>()
						.WhereContains(ToDispatch, true).ToList();

					return commits.Select(x => x.ToCommit());
				}
				catch (Exception e)
				{
					throw new PersistenceEngineException(e.Message, e);
				}
			}
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				var patch = commit.ToRavenCommit().RemoveProperty(ToDispatch);
				session.Advanced.DatabaseCommands.Batch(new[] { patch });
				session.SaveChanges();
			}
		}

		public virtual IEnumerable<StreamToSnapshot> GetStreamsToSnapshot(int maxThreshold)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			{
				return null;
			}
		}
		public virtual void AddSnapshot(Guid streamId, long streamRevision, object snapshot)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			{
				// update commit with a specific snapshot
			}
		}
	}
}