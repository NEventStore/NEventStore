namespace EventStore.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Persistence;
	using Raven.Client;
	using Raven.Client.Exceptions;

	public class RavenPersistenceEngine : IPersistStreams
	{
		private readonly IDocumentStore store;

		public RavenPersistenceEngine(IDocumentStore store)
		{
			this.store = store;
			this.store.Initialize();
		}

		public virtual IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			return null;
		}
		public virtual IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			using (var session = this.store.OpenSession())
			{
				session.Advanced.AllowNonAuthoritiveInformation = false;

				try
				{
					return (from commit in session.Query<RavenCommit>()
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
			using (var session = this.store.OpenSession())
			{
				var patch = commit.RemoveUndispatchedProperty();
				session.Advanced.DatabaseCommands.Batch(new[] { patch });
				session.SaveChanges();
			}
		}

		public virtual IEnumerable<Guid> GetStreamsToSnapshot(int maxThreshold)
		{
			return null;
		}
		public virtual void AddSnapshot(Guid streamId, long commitSequence, object snapshot)
		{
			// inserts a snapshot document *between* two commits
		}
	}
}