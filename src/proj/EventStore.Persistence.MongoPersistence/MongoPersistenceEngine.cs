namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using MongoDB.Bson;
	using MongoDB.Driver;
	using MongoDB.Driver.Builders;
	using Serialization;

	public class MongoPersistenceEngine : IPersistStreams
	{
		private const string ConcurrencyException = "E1100";
		private readonly MongoDatabase store;
		private readonly IDocumentSerializer serializer;
		private bool disposed;
		private int initialized;
		private readonly MongoCollectionSettings<BsonDocument> _persistedCommitsSettings;
		private readonly MongoCollectionSettings<BsonDocument> _persistedSnapshotsSettings;
		private readonly MongoCollectionSettings<BsonDocument> _persistedStreamHeadsSettings;

		public MongoPersistenceEngine(MongoDatabase store, IDocumentSerializer serializer)
		{
			this.store = store;
			this.serializer = serializer;

			_persistedCommitsSettings = this.store.CreateCollectionSettings<BsonDocument>("Commits");
			_persistedCommitsSettings.AssignIdOnInsert = false;
			_persistedCommitsSettings.SafeMode = SafeMode.True;

			_persistedSnapshotsSettings = this.store.CreateCollectionSettings<BsonDocument>("Snapshots");
			_persistedSnapshotsSettings.AssignIdOnInsert = false;
			_persistedSnapshotsSettings.SafeMode = SafeMode.True;

			_persistedStreamHeadsSettings = this.store.CreateCollectionSettings<BsonDocument>("Streams");
			_persistedStreamHeadsSettings.AssignIdOnInsert = false;
			_persistedStreamHeadsSettings.SafeMode = SafeMode.False;
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
			if (Interlocked.Increment(ref this.initialized) > 1)
				return;

			this.TryMongo(() =>
			{
				this.PersistedCommits.EnsureIndex(
				IndexKeys.Ascending("Dispatched").Ascending("CommitStamp"),
				IndexOptions.SetName("Dispatched_Index").SetUnique(false));

				this.PersistedCommits.EnsureIndex(
					IndexKeys.Ascending("_id.StreamId", "Events.StreamRevision", "_id.CommitSequence"),
					IndexOptions.SetName("GetFrom_Index").SetUnique(true));

				this.PersistedCommits.EnsureIndex(
					IndexKeys.Ascending("CommitStamp"),
					IndexOptions.SetName("CommitStamp_Index").SetUnique(false));
			});
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			return this.TryMongo(() =>
			{
				var query = Query.And(
					Query.EQ("_id.StreamId", streamId),
					Query.GTE("Events.StreamRevision", minRevision),
					Query.LTE("Events.StreamRevision", maxRevision));

				return this.PersistedCommits
					.Find(query)
					.SetFlags(QueryFlags.NoCursorTimeout | QueryFlags.Exhaust)
					.SetSortOrder("_id.CommitSequence")
					.Select(mc => mc.ToCommit(this.serializer))
					.ToArray();
			});
		}
		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			return this.TryMongo(() => this.PersistedCommits
				.Find(Query.GTE("CommitStamp", start))
				.SetSortOrder("CommitStamp")
				.Select(x => x.ToCommit(this.serializer))
				.ToArray());
		}

		public virtual void Commit(Commit attempt)
		{
			this.TryMongo(() =>
			{
				var commit = attempt.ToMongoCommit(this.serializer);

				try
				{
					// for concurrency / duplicate commit detection safe mode is required
					this.PersistedCommits.Insert(commit, SafeMode.True);
					this.UpdateStreamHeadAsync(attempt.StreamId, attempt.StreamRevision, (attempt.CommitSequence == 1));
				}
				catch (MongoException e)
				{
					if (!e.Message.Contains(ConcurrencyException))
						throw;

					var committed = this.PersistedCommits.FindOne(attempt.ToMongoCommitIdQuery()).ToCommit(this.serializer);
					if (committed == null || committed.CommitId == attempt.CommitId)
						throw new DuplicateCommitException();

					throw new ConcurrencyException();
				}
			});
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.TryMongo(() => this.PersistedCommits
				.Find(Query.EQ("Dispatched", false))
				.SetSortOrder("CommitStamp")
				.Select(mc => mc.ToCommit(this.serializer))
				.ToArray());
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.TryMongo(() =>
			{
				var query = commit.ToMongoCommitIdQuery();
				var update = Update.Set("Dispatched", true);
				this.PersistedCommits.Update(query, update);
			});
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.TryMongo(() =>
			{
				var query = Query
				.Where(BsonJavaScript.Create("this.HeadRevision >= this.SnapshotRevision + " + maxThreshold));

				return this.PersistedStreamHeads
					.Find(query)
					.ToArray()
					.Select(x => x.ToStreamHead())
					.ToArray();
			});
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			return this.TryMongo(() => this.PersistedSnapshots
				.Find(streamId.ToSnapshotQuery(maxRevision))
				.SetSortOrder(SortBy.Descending("_id"))
				.SetLimit(1)
				.Select(mc => mc.ToSnapshot(this.serializer))
				.FirstOrDefault());
		}

		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			if (snapshot == null)
				return false;

			try
			{
				var mongoSnapshot = snapshot.ToMongoSnapshot(this.serializer);
				this.PersistedSnapshots.Insert(mongoSnapshot);
				this.PersistedStreamHeads.Update(
					Query.EQ("_id", snapshot.StreamId),
					Update.Set("SnapshotRevision", snapshot.StreamRevision));

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private void UpdateStreamHeadAsync(Guid streamId, int streamRevision, bool isFirstCommit)
		{
			ThreadPool.QueueUserWorkItem(x => this.TryMongo(() =>
			{
				if (isFirstCommit)
					this.PersistedStreamHeads.Insert(
						new BsonDocument
							{
								{ "_id", streamId },
								{ "HeadRevision", streamRevision },
								{ "SnapshotRevision", 0 }
							});
				else
					this.PersistedStreamHeads.Update(
						Query.EQ("_id", streamId),
						Update.Set("HeadRevision", streamRevision));
			}), null);
		}

		protected virtual MongoCollection<BsonDocument> PersistedCommits
		{
			get { return this.store.GetCollection(_persistedCommitsSettings); }
		}
		protected virtual MongoCollection<BsonDocument> PersistedSnapshots
		{
			get { return this.store.GetCollection(_persistedSnapshotsSettings); }
		}
		protected virtual MongoCollection<BsonDocument> PersistedStreamHeads
		{
			get { return this.store.GetCollection(_persistedStreamHeadsSettings); }
		}

		protected virtual T TryMongo<T>(Func<T> callback)
		{
			var results = default(T);

			this.TryMongo(() =>
			{
				results = callback();
			});

			return results;
		}
		protected virtual void TryMongo(Action callback)
		{
			try
			{
				callback();
			}
			catch (MongoConnectionException e)
			{
				throw new StorageUnavailableException(e.Message, e);
			}
			catch (MongoException e)
			{
				throw new StorageException(e.Message, e);
			}
		}
	}
}