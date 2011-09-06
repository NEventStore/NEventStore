namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Net;
	using System.Threading;
	using System.Transactions;
	using Indexes;
	using Raven.Abstractions.Commands;
	using Raven.Abstractions.Data;
	using Raven.Client;
	using Raven.Client.Connection;
	using Raven.Client.Exceptions;
	using Raven.Client.Indexes;
	using Raven.Json.Linq;
	using Serialization;

	public class RavenPersistenceEngine : IPersistStreams
	{
		private const int MinPageSize = 10;
		private readonly IDocumentStore store;
		private readonly IDocumentSerializer serializer;
		private readonly TransactionScopeOption scopeOption;
		private readonly bool consistentQueries;
		private readonly int pageSize;
		private bool disposed;
		private int initialized;

		public RavenPersistenceEngine(IDocumentStore store, RavenConfiguration config)
		{
			if (store == null)
				throw new ArgumentNullException("store");

			if (config == null)
				throw new ArgumentNullException();

			if (config.Serializer == null)
				throw new ArgumentException("Serializer cannot be null.", "config");

			if (config.PageSize < MinPageSize)
				throw new ArgumentException("Configured paging size is too small.", "config");

			this.store = store;
			this.serializer = config.Serializer;
			this.scopeOption = config.ScopeOption;
			this.consistentQueries = config.ConsistentQueries;
			this.pageSize = config.PageSize;
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
			if (Interlocked.Increment(ref this.initialized) > 1)
				return;

			try
			{
				using (var scope = this.OpenCommandScope())
				{
					new RavenCommitByDate().Execute(this.store);
					new RavenCommitByRevisionRange().Execute(this.store);
					new RavenCommitsByDispatched().Execute(this.store);
					new RavenSnapshotByStreamIdAndRevision().Execute(this.store);
					new RavenStreamHeadBySnapshotAge().Execute(this.store);
					scope.Complete();
				}
			}
			catch (WebException e)
			{
				throw new StorageUnavailableException(e.Message, e);
			}
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			return this.QueryCommits<RavenCommitByRevisionRange>(x =>
					x.StreamId == streamId && x.StreamRevision >= minRevision && x.StartingStreamRevision <= maxRevision)
				.OrderBy(x => x.CommitSequence);
		}
		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			return this.QueryCommits<RavenCommitByDate>(x => x.CommitStamp >= start)
				.OrderBy(x => x.CommitStamp);
		}
		public virtual void Commit(Commit attempt)
		{
			try
			{
				using (var scope = this.OpenCommandScope())
				using (var session = this.store.OpenSession())
				{
					session.Advanced.UseOptimisticConcurrency = true;
					session.Store(attempt.ToRavenCommit(this.serializer));
					session.SaveChanges();
					scope.Complete();
				}

				this.SaveStreamHead(attempt.ToRavenStreamHead());
			}
			catch (WebException e)
			{
				throw new StorageUnavailableException(e.Message, e);
			}
			catch (NonUniqueObjectException e)
			{
				throw new DuplicateCommitException(e.Message, e);
			}
			catch (Raven.Abstractions.Exceptions.ConcurrencyException)
			{
				var savedCommit = this.LoadSavedCommit(attempt);
				if (savedCommit.CommitId == attempt.CommitId)
					throw new DuplicateCommitException();

				throw new ConcurrencyException();
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.QueryCommits<RavenCommitsByDispatched>(c => c.Dispatched == false)
				.OrderBy(x => x.CommitStamp);
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			if (commit == null)
				throw new ArgumentNullException("commit");

			var patch = new PatchRequest
			{
				Type = PatchCommandType.Set,
				Name = "Dispatched",
				Value = RavenJToken.Parse("true")
			};
			var data = new PatchCommandData
			{
				Key = commit.ToRavenCommitId(),
				Patches = new[] { patch }
			};

			try
			{
				using (var scope = this.OpenCommandScope())
				using (var session = this.store.OpenSession())
				{
					session.Advanced.DatabaseCommands.Batch(new[] { data });
					session.SaveChanges();
					scope.Complete();
				}
			}
			catch (WebException e)
			{
				throw new StorageUnavailableException(e.Message, e);
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.Query<RavenStreamHead, RavenStreamHeadBySnapshotAge>(s => s.SnapshotAge >= maxThreshold)
				.Select(s => s.ToStreamHead());
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			return this.Query<RavenSnapshot, RavenSnapshotByStreamIdAndRevision>(
				x => x.StreamId == streamId && x.StreamRevision <= maxRevision)
				.OrderByDescending(x => x.StreamRevision)
				.FirstOrDefault()
				.ToSnapshot(this.serializer);
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			if (snapshot == null)
				return false;

			try
			{
				using (var scope = this.OpenCommandScope())
				using (var session = this.store.OpenSession())
				{
					var ravenSnapshot = snapshot.ToRavenSnapshot(this.serializer);
					session.Store(ravenSnapshot);
					session.SaveChanges();
					scope.Complete();
				}

				this.SaveStreamHead(snapshot.ToRavenStreamHead());

				return true;
			}
			catch (WebException e)
			{
				throw new StorageUnavailableException(e.Message, e);
			}
			catch (Raven.Abstractions.Exceptions.ConcurrencyException)
			{
				return false;
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		public virtual void Purge()
		{
			try
			{
				using (var scope = this.OpenCommandScope())
				using (var session = this.store.OpenSession())
				{
					var cmd = session.Advanced.DatabaseCommands;
					PurgeCollection(cmd, "Tag:[[RavenCommits]]");
					PurgeCollection(cmd, "Tag:[[RavenSnapshots]]");
					PurgeCollection(cmd, "Tag:[[RavenStreamHeads]]");

					session.SaveChanges();
					scope.Complete();
				}
			}
			catch (WebException e)
			{
				throw new StorageUnavailableException(e.Message, e);
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}
		private static void PurgeCollection(IDatabaseCommands commands, string tag)
		{
			commands.DeleteByIndex("Raven/DocumentsByEntityName", new IndexQuery { Query = tag }, true);
		}

		private RavenCommit LoadSavedCommit(Commit attempt)
		{
			try
			{
				using (this.OpenQueryScope())
				using (var session = this.store.OpenSession())
					return session.Load<RavenCommit>(attempt.ToRavenCommitId());
			}
			catch (WebException e)
			{
				throw new StorageUnavailableException(e.Message, e);
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}
		private IEnumerable<Commit> QueryCommits<TIndex>(Expression<Func<RavenCommit, bool>> query)
			where TIndex : AbstractIndexCreationTask, new()
		{
			return this.Query<RavenCommit, TIndex>(query).Select(x => x.ToCommit(this.serializer));
		}
		private IEnumerable<T> Query<T, TIndex>(Expression<Func<T, bool>> query)
			where TIndex : AbstractIndexCreationTask, new()
		{
			try
			{
				using (this.OpenQueryScope())
				using (var session = this.OpenQuerySession())
					return session.Query<T, TIndex>()
						.Customize(x => { if (this.consistentQueries) x.WaitForNonStaleResults(); })
						.Where(query)
						.Page(this.pageSize);
			}
			catch (WebException e)
			{
				throw new StorageUnavailableException(e.Message, e);
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		protected virtual IDocumentSession OpenQuerySession()
		{
			var session = this.store.OpenSession();

			// defaults to 30 total requests per session (not good for paging over large data sets)
			// which may be encountered when calling GetFrom() and enumerating over the entire store.
			// see http://ravendb.net/documentation/safe-by-default for more information.
			session.Advanced.MaxNumberOfRequestsPerSession = int.MaxValue;

			return session;
		}

		private void SaveStreamHead(RavenStreamHead streamHead)
		{
			if (this.consistentQueries)
				this.SaveStreamHeadAsync(streamHead);
			else
				ThreadPool.QueueUserWorkItem(x => this.SaveStreamHeadAsync(streamHead), null);
		}
		private void SaveStreamHeadAsync(RavenStreamHead updated)
		{
			using (var scope = this.OpenCommandScope())
			using (var session = this.store.OpenSession())
			{
				var current = session.Load<RavenStreamHead>(updated.StreamId.ToRavenStreamId()) ?? updated;
				current.HeadRevision = updated.HeadRevision;

				if (updated.SnapshotRevision > 0)
					current.SnapshotRevision = updated.SnapshotRevision;

				session.Advanced.UseOptimisticConcurrency = false;
				session.Store(current);
				session.SaveChanges();
				scope.Complete(); // if this fails it's no big deal, stream heads can be updated whenever
			}
		}
		protected virtual TransactionScope OpenQueryScope()
		{
			return this.OpenCommandScope();
		}
		protected virtual TransactionScope OpenCommandScope()
		{
			return new TransactionScope(this.scopeOption);
		}
	}
}