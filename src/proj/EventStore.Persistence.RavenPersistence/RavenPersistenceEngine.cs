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

		public RavenPersistenceEngine(IDocumentStore store)
		{
			this.store = store;
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
					return (from commit in session.Query<RavenCommit>().Customize(x => x.WaitForNonStaleResultsAsOfNow())
					        where commit.StreamId == streamId
					              && commit.StreamRevision >= minRevision
					              && commit.Snapshot == null
					        select commit.ToCommit()).ToArray();
				}
				catch (Exception e)
				{
					throw new PersistenceException(e.Message, e);
				}
			}
		}
		public virtual void Persist(CommitAttempt uncommitted)
		{
			using (new TransactionScope(TransactionScopeOption.Suppress))
			using (var session = this.store.OpenSession())
			{
				session.Advanced.UseOptimisticConcurrency = true;
				session.Store(new RavenCommit(uncommitted));

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
					throw new PersistenceException(e.Message, e);
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
					return session.Query<RavenCommit>()
						.Where(x => x.PendingDispatch)
						.Select(x => x.ToCommit())
						.ToArray();
				}
				catch (Exception e)
				{
					throw new PersistenceException(e.Message, e);
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