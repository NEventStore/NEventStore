namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Transactions;
	using Persistence;
	using Raven.Client;
	using Raven.Client.Exceptions;

	public class RavenPersistenceEngine : IPersistStreams
	{
		private readonly IDocumentStore store;
		private readonly IInitializeRaven initializer;

		public RavenPersistenceEngine(
			IDocumentStore store, IInitializeRaven initializer)
		{
			this.store = store;
			this.initializer = initializer;
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
				session.Advanced.AllowNonAuthoritiveInformation = false;

				try
				{
					return (from commit in session.Query<Commit>().Customize(x => x.WaitForNonStaleResultsAsOfNow())
					        where commit.StreamId == streamId
					              && commit.StreamRevision >= minRevision
					              && commit.Snapshot == null
					        select commit).ToArray();
				}
				catch (Exception e)
				{
					throw new PersistenceEngineException(e.Message, e);
				}
			}
		}
		public virtual void Persist(CommitAttempt uncommitted)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				session.Advanced.UseOptimisticConcurrency = true;
				session.Store(uncommitted.ToCommit());

				try
				{
					session.SaveChanges();
				}
				catch (ConflictException e)
				{
					throw new ConcurrencyException(e.Message, e);
				}
				catch (NonUniqueObjectException e)
				{
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
					// TODO
					return session.Query<Commit>().Where(x => false).ToArray();
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
				var patch = commit.RemoveUndispatchedProperty();
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
				// inserts a snapshot document *between* two commits
			}
		}
	}
}