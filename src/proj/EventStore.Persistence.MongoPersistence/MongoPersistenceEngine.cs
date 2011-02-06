namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using MongoDB.Bson;
	using MongoDB.Driver;
	using MongoDB.Driver.Builders;
	using Serialization;

	public class MongoPersistenceEngine : IPersistStreams
	{
		private const string ConcurrencyException = "E1100";
		private readonly MongoDatabase store;
		private readonly ISerialize serializer;
		private bool disposed;

		public MongoPersistenceEngine(MongoDatabase store, ISerialize serializer)
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

		public virtual void Initialize()
		{
			this.PersistedCommits.EnsureIndex(
				IndexKeys.Ascending("Dispatched").Ascending("CommitStamp"),
				IndexOptions.SetName("Dispatched_Index").SetUnique(false));

			this.PersistedCommits.EnsureIndex(
				IndexKeys.Ascending("_id.StreamId", "StartingStreamRevision", "StreamRevision"),
				IndexOptions.SetName("GetFrom_Index").SetUnique(true));

			this.PersistedCommits.EnsureIndex(
				IndexKeys.Ascending("CommitStamp"),
				IndexOptions.SetName("CommitStamp_Index").SetUnique(false));
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			try
			{
				var query = Query.And(
					Query.EQ("_id.StreamId", streamId),
					Query.GTE("StreamRevision", minRevision),
					Query.LTE("StartingStreamRevision", maxRevision));

				return this.PersistedCommits
					.Find(query)
					.SetSortOrder("StartingStreamRevision")
					.Select(mc => mc.ToCommit(this.serializer));
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

				return this.PersistedCommits
					.Find(query)
					.SetSortOrder("CommitStamp")
					.Select(x => x.ToCommit(this.serializer));
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		public virtual void Commit(Commit attempt)
		{
			var commit = attempt.ToMongoCommit(this.serializer);

			try
			{
				// for concurrency / duplicate commit detection safe mode is required
				this.PersistedCommits.Insert(commit, SafeMode.True);

				var head = new MongoStreamHead(commit.Id.StreamId, commit.StreamRevision, 0);
				this.SaveStreamHeadAsync(head);
			}
			catch (MongoException e)
			{
				if (!e.Message.Contains(ConcurrencyException))
					throw new StorageException(e.Message, e);

				var committed = this.PersistedCommits.FindOne(commit.ToMongoCommitIdQuery());
				if (committed == null || committed.CommitId == commit.CommitId)
					throw new DuplicateCommitException();

				throw new ConcurrencyException();
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			var query = Query.EQ("Dispatched", false);

			return this.PersistedCommits
				.Find(query)
				.SetSortOrder("CommitStamp")
				.Select(mc => mc.ToCommit(this.serializer));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			var query = commit.ToMongoCommitIdQuery();
			var update = Update.Set("Dispatched", true);
			this.PersistedCommits.Update(query, update);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			var query = Query
				.Where(BsonJavaScript.Create("this.HeadRevision >= this.SnapshotRevision + " + maxThreshold));

			return this.PersistedStreamHeads
				.Find(query)
				.ToArray()
				.Select(x => x.ToStreamHead());
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			return this.PersistedSnapshots
				.FindAs<BsonDocument>(streamId.ToSnapshotQuery(maxRevision))
				.SetSortOrder(SortBy.Descending("_id"))
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
				var mongoSnapshot = snapshot.ToMongoSnapshot(this.serializer);
				this.PersistedSnapshots.Insert(mongoSnapshot);

				var head = new MongoStreamHead(snapshot.StreamId, snapshot.StreamRevision, snapshot.StreamRevision);
				this.SaveStreamHeadAsync(head);

				return true;
			}
			catch (MongoException)
			{
				return false;
			}
		}

		private void SaveStreamHeadAsync(MongoStreamHead streamHead)
		{
			// ThreadPool.QueueUserWorkItem(item => this.PersistedStreamHeads.Save(item as StreamHead), streamHead);
			var query = Query.EQ("_id", streamHead.StreamId);
			var update = Update
				.Set("HeadRevision", streamHead.HeadRevision)
				.Set("SnapshotRevision", streamHead.SnapshotRevision);

			this.PersistedStreamHeads.Update(query, update, UpdateFlags.Upsert);
		}

		private MongoCollection<MongoCommit> PersistedCommits
		{
			get { return this.store.GetCollection<MongoCommit>("Commits"); }
		}
		private MongoCollection<MongoSnapshot> PersistedSnapshots
		{
			get { return this.store.GetCollection<MongoSnapshot>("Snapshots"); }
		}
		private MongoCollection<MongoStreamHead> PersistedStreamHeads
		{
			get { return this.store.GetCollection<MongoStreamHead>("Streams"); }
		}
	}
}