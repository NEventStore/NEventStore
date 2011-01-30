namespace EventStore.Persistence.MongoDBPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using MongoDB.Bson;
	using MongoDB.Driver;
	using MongoDB.Driver.Builders;
	using Serialization;

	public class MongoDBPersistenceEngine : IPersistStreams
	{
		private const string ConcurrencyException = "E1100";
		private readonly MongoDatabase store;
		private readonly ISerialize serializer;
		private bool disposed;

		public MongoDBPersistenceEngine(MongoDatabase store, ISerialize serializer)
		{
			this.store = store;
			this.serializer = serializer;
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
		}

		private MongoCollection<MongoDBCommit> PersistedCommits
		{
			get { return this.store.GetCollection<MongoDBCommit>("Commit"); }
		}

		private MongoCollection<MongoDBSnapshot> PersistedSnapshots
		{
			get { return this.store.GetCollection<MongoDBSnapshot>("Snapshot"); }
		}

		public virtual void Initialize()
		{
			this.PersistedCommits.EnsureIndex(
				IndexKeys.Ascending("Dispatched"), 
				IndexOptions.SetName("Dispatched_Index").SetUnique(false));

			this.PersistedCommits.EnsureIndex(
				IndexKeys.Ascending("_id.StreamId", "MinStreamRevision", "MaxStreamRevision"),
				IndexOptions.SetName("GetFrom_Index").SetUnique(true));
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			try
			{
				var query = Query.And(
					Query.EQ("_id.StreamId", streamId),
					Query.GTE("MaxStreamRevision", minRevision),
					Query.LTE("MinStreamRevision", maxRevision)
				);

				return this.PersistedCommits.Find(query).SetSortOrder("MinStreamRevision").Select(mc => mc.ToCommit(this.serializer));
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			try
			{
				var query = Query.GTE("CommitStamp", start);

				return this.PersistedCommits.Find(query).SetSortOrder("CommitStamp").Select(mc => mc.ToCommit(this.serializer));
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		public virtual void Commit(Commit attempt)
		{
			var commit = attempt.ToMongoDBCommit(this.serializer);

			try
			{
				// if concurrency / duplicate commit detection is required then safe mode is required
				this.PersistedCommits.Insert(commit, SafeMode.True);	// TODO: update associated StreamHead--should be done asynchronously.
			}
			catch (MongoException e)
			{
				if (!e.Message.Contains(ConcurrencyException))
					throw new StorageException(e.Message, e);

				var committed = this.PersistedCommits.FindOne(commit.ToMongoDBCommitIdQuery());
				if (committed != null && committed.CommitId != commit.CommitId)
					throw new ConcurrencyException();

				throw new DuplicateCommitException();
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			var query = Query.EQ("Dispatched", false);

			return this.PersistedCommits.Find(query).SetSortOrder("CommitStamp").Select(mc => mc.ToCommit(this.serializer));
		}

		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			var query = commit.ToMongoDBCommitIdQuery();
			var update = Update.Set("Dispatched", true);
			this.PersistedCommits.Update(query, update);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return new StreamHead[0]; // TODO: query StreamHead documents to determine which streams should be snapshot.
		}

		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			var query = Query.And(
					Query.EQ("_id.StreamId", streamId),
					Query.LTE("_id.StreamRevision", maxRevision)
				);

			return this.PersistedSnapshots
					.Find(query)
					.SetSortOrder(SortBy.Descending("_id.StreamRevision"))
					.SetLimit(1)
					.Select(mc => mc.ToSnapshot(this.serializer))
					.FirstOrDefault();
		}

		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			if (snapshot == null)
				return false;

			try
			{
				var mongoSnapshot = snapshot.ToMongoDBSnapshot(this.serializer);
				this.PersistedSnapshots.Insert(mongoSnapshot); // TODO: update associated StreamHead--should be done asynchronously.
				return true;
			}
			catch (MongoException e)
			{
				if (!e.Message.StartsWith(ConcurrencyException))
					throw new StorageException(e.Message, e);

				return false;
			}
		}
	}
}