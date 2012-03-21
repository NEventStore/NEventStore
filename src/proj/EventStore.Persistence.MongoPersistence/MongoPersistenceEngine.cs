using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using Logging;
	using MongoDB.Bson;
	using MongoDB.Driver;
	using MongoDB.Driver.Builders;
	using Serialization;

	public class MongoPersistenceEngine : IPersistStreams
	{
		private const string ConcurrencyException = "E1100";
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(MongoPersistenceEngine));
		private readonly MongoCollectionSettings<BsonDocument> commitSettings;
		private readonly MongoCollectionSettings<BsonDocument> snapshotSettings;
		private readonly MongoCollectionSettings<BsonDocument> streamSettings;
		private readonly MongoDatabase store;
		private readonly IDocumentSerializer serializer;
		private readonly BlockingCollection<StreamHeadUpdateInfo> streamHeadUpdateQueue;
		private readonly BlockingCollection<Guid> updateDispatchedQueue;
		private readonly Task streamHeadUpdateTask;
		private readonly Task updateDispatchedTask;
		private readonly SnapshotTracking snapshotTracking; 
		private bool disposed;
		private int initialized;

		public MongoPersistenceEngine(MongoDatabase store, IDocumentSerializer serializer, SnapshotTracking snapshotTracking)
		{
			if (store == null)
				throw new ArgumentNullException("store");

			if (serializer == null)
				throw new ArgumentNullException("serializer");

			this.store = store;
			this.serializer = serializer;
			this.snapshotTracking = snapshotTracking;

			this.commitSettings = this.store.CreateCollectionSettings<BsonDocument>("Commits");
			this.commitSettings.AssignIdOnInsert = false;
			this.commitSettings.SafeMode = SafeMode.True;

			this.snapshotSettings = this.store.CreateCollectionSettings<BsonDocument>("Snapshots");
			this.snapshotSettings.AssignIdOnInsert = false;
			this.snapshotSettings.SafeMode = SafeMode.False;

			this.updateDispatchedQueue = new BlockingCollection<Guid>();
			this.updateDispatchedTask = Task.Factory.StartNew(UpdateDispatchedFlag, TaskCreationOptions.LongRunning);

			if (snapshotTracking == SnapshotTracking.Enabled)
			{
				this.streamSettings = this.store.CreateCollectionSettings<BsonDocument>("Streams");
				this.streamSettings.AssignIdOnInsert = false;
				this.streamSettings.SafeMode = SafeMode.False;

				this.streamHeadUpdateQueue = new BlockingCollection<StreamHeadUpdateInfo>();
				this.streamHeadUpdateTask = Task.Factory.StartNew(UpdateStreamHead, TaskCreationOptions.LongRunning);
			}
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

			Logger.Debug(Messages.ShuttingDownPersistence);
			
			// the update tasks will complete when the queues are emptied
			this.updateDispatchedQueue.CompleteAdding();
			this.updateDispatchedTask.Wait();
			this.updateDispatchedTask.Dispose();
			this.updateDispatchedQueue.Dispose();

			if (snapshotTracking == SnapshotTracking.Enabled)
			{
				this.streamHeadUpdateQueue.CompleteAdding();
				this.streamHeadUpdateTask.Wait();
				this.streamHeadUpdateTask.Dispose();
				this.streamHeadUpdateQueue.Dispose();
			}

			this.disposed = true;
		}

		public virtual void Initialize()
		{
			if (Interlocked.Increment(ref this.initialized) > 1)
				return;

			Logger.Debug(Messages.InitializingStorage);

			this.TryMongo(() =>
			{
				this.PersistedCommits.EnsureIndex(
					IndexKeys.Ascending("d"),
					IndexOptions.SetName("Dispatch"));

				this.PersistedCommits.EnsureIndex(
					IndexKeys.Ascending("i", "n"),
					IndexOptions.SetName("UniqueCommit").SetUnique(true));

				this.PersistedCommits.EnsureIndex(
					IndexKeys.Ascending("i", "e.r"),
					IndexOptions.SetName("GetFromRevision"));

				this.PersistedCommits.EnsureIndex(
					IndexKeys.Ascending("s"),
					IndexOptions.SetName("GetFromDate"));

				if (snapshotTracking == SnapshotTracking.Enabled)
				{
					this.PersistedStreamHeads.EnsureIndex(
					IndexKeys.Ascending("u"),
					IndexOptions.SetName("Unsnapshotted"));
				}
			});
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			Logger.Debug(Messages.GettingAllCommitsBetween, streamId, minRevision, maxRevision);

			return this.TryMongo(() =>
			{
				IMongoQuery query;
				MongoCursor<BsonDocument> cursor;

				if (minRevision == maxRevision)
				{
					// getting a specific revision (no range query, no sort required)
					query = Query.And(Query.EQ("i", streamId), Query.EQ("e.r", minRevision));
					cursor = this.PersistedCommits.Find(query);
				}
				else if (maxRevision == int.MaxValue)
				{
					// getting everying from the minimum revision - no upper limit needed but sort required
					query = Query.And(Query.EQ("i", streamId), Query.GTE("e.r", minRevision));
					cursor = this.PersistedCommits.Find(query).SetSortOrder("e.r");
				}
				else if (minRevision <= 1)
				{
					// getting everying up to the maximum revision - no lower limit needed but sort required
					query = Query.And(Query.EQ("i", streamId), Query.LTE("e.r", maxRevision));
					cursor = this.PersistedCommits.Find(query).SetSortOrder("e.r");
				}
				else
				{
					// getting a range - use min and max functions instead of LTE / GTE (more consistently optimal)
					cursor = this.PersistedCommits
						.FindAll()
						.SetMin(Query.And(Query.EQ("i", streamId), Query.EQ("e.r", minRevision)).ToBsonDocument())
						.SetMax(Query.And(Query.EQ("i", streamId), Query.EQ("e.r", maxRevision + 1)).ToBsonDocument())
						.SetSortOrder("e.r");
				}

				return cursor.Select(mc => mc.ToCommit(this.serializer));
			});
		}
		public virtual IEnumerable<Commit> GetFrom(DateTime start)
		{
			Logger.Debug(Messages.GettingAllCommitsFrom, start);

			return this.TryMongo(() => this.PersistedCommits
				.Find(Query.GTE("s", start))
				.SetSortOrder("s")
				.Select(x => x.ToCommit(this.serializer)));
		}
		public virtual void Commit(Commit attempt)
		{
			Logger.Debug(Messages.AttemptingToCommit,
				attempt.Events.Count, attempt.StreamId, attempt.CommitSequence);

			this.TryMongo(() =>
			{
				var commit = attempt.ToMongoCommit(this.serializer);

				try
				{
					// for concurrency / duplicate commit detection safe mode is required
					this.PersistedCommits.Insert(commit, SafeMode.True);
					if (snapshotTracking == SnapshotTracking.Enabled)
					{
						this.streamHeadUpdateQueue.Add(new StreamHeadUpdateInfo(attempt.StreamId, attempt.Events.Count));
					}
					Logger.Debug(Messages.CommitPersisted, attempt.CommitId);
				}
				catch (MongoException e)
				{
					if (!e.Message.Contains(ConcurrencyException))
						throw;

					var savedCommit = this.PersistedCommits.FindOne(attempt.ToMongoCommitIdQuery()).ToCommit(this.serializer);
					if (savedCommit.CommitId == attempt.CommitId)
						throw new DuplicateCommitException();

					Logger.Debug(Messages.ConcurrentWriteDetected);
					throw new ConcurrencyException();
				}
			});
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			Logger.Debug(Messages.GettingUndispatchedCommits);

			return this.TryMongo(() => this.PersistedCommits
				.Find(Query.EQ("d", false))
				.SetSortOrder("s")
				.Select(mc => mc.ToCommit(this.serializer)));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.updateDispatchedQueue.Add(commit.CommitId);
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			Logger.Debug(Messages.GettingStreamsToSnapshot);

			if (snapshotTracking == SnapshotTracking.Disabled)
			{
				throw new NotSupportedException(Messages.SnapshotPolicyDisabled);
			}

			return this.TryMongo(() =>
			{
				var query = Query.GTE("u", maxThreshold);

				return this.PersistedStreamHeads
					.Find(query)
					.SetSortOrder(SortBy.Descending("u"))
					.Select(x => x.ToStreamHead());
			});
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			Logger.Debug(Messages.GettingRevision, streamId, maxRevision);

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

			Logger.Debug(Messages.AddingSnapshot, snapshot.StreamId, snapshot.StreamRevision);

			try
			{
				var mongoSnapshot = snapshot.ToMongoSnapshot(this.serializer);
				var query = Query.EQ("_id", mongoSnapshot["_id"]);
				var update = Update.Set("p", mongoSnapshot["p"]);

				// Doing an upsert instead of an insert allows us to overwrite an existing snapshot and not get stuck with a
				// stream that needs to be snapshotted because the insert fails and the SnapshotRevision isn't being updated.
				this.PersistedSnapshots.Update(query, update, UpdateFlags.Upsert);

				if (snapshotTracking == SnapshotTracking.Enabled)
				{
					// More commits could have been made between us deciding that a snapshot is required and writing it so just 
					// resetting the Unsnapshotted count may be a little off - we need to adjust based on the previous streamhead
					// snapshot value.
					var streamHead = this.PersistedStreamHeads.FindOneById(snapshot.StreamId).ToStreamHead();
					var adjustment = streamHead.SnapshotRevision - snapshot.StreamRevision;
					this.PersistedStreamHeads.Update(
						Query.EQ("_id", snapshot.StreamId),
						Update.Set("s", snapshot.StreamRevision).Inc("u", adjustment));
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public virtual void Purge()
		{
			Logger.Warn(Messages.PurgingStorage);

			this.PersistedCommits.Drop();
			this.PersistedSnapshots.Drop();
			if (snapshotTracking == SnapshotTracking.Enabled)
			{
				this.PersistedStreamHeads.Drop();
			}
		}

		private void UpdateStreamHead()
		{
			// pin a database connection for use on this thread to prevent stream updates slowing down commits
			using (this.PersistedStreamHeads.Database.RequestStart(false))
			{
				foreach (var streamHeadUpdateInfo in this.streamHeadUpdateQueue.GetConsumingEnumerable())
				{
					var info = streamHeadUpdateInfo;
					this.TryMongo(() =>
					{
						this.PersistedStreamHeads.Update(
							Query.EQ("_id", info.StreamId),
							Update.Inc("h", info.EventCount).Inc("s", 0).Inc("u", info.EventCount),
							UpdateFlags.Upsert,
							SafeMode.False);
					});
				}
			}
		}
		private void UpdateDispatchedFlag()
		{
			// pin a database connection for use on this thread to prevent dispatched updates slowing down commits
			using (this.PersistedCommits.Database.RequestStart(false))
			{
				foreach (var commitId in this.updateDispatchedQueue.GetConsumingEnumerable())
				{
					var id = commitId;
					Logger.Debug(Messages.MarkingCommitAsDispatched, id);

					this.TryMongo(() =>
					{
						var query = Query.EQ("_id", id);
						var update = Update.Set("d", true);
						this.PersistedCommits.Update(query, update, SafeMode.False);
					});
				}
			}
		}

		protected virtual MongoCollection<BsonDocument> PersistedCommits
		{
			get { return this.store.GetCollection(this.commitSettings); }
		}
		protected virtual MongoCollection<BsonDocument> PersistedStreamHeads
		{
			get { return this.store.GetCollection(this.streamSettings); }
		}
		protected virtual MongoCollection<BsonDocument> PersistedSnapshots
		{
			get { return this.store.GetCollection(this.snapshotSettings); }
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
			if (this.disposed)
				throw new ObjectDisposedException("Attempt to use storage after it has been disposed.");

			try
			{
				callback();
			}
			catch (MongoConnectionException e)
			{
				Logger.Warn(Messages.StorageUnavailable);
				throw new StorageUnavailableException(e.Message, e);
			}
			catch (MongoException e)
			{
				Logger.Error(Messages.StorageThrewException, e.GetType());
				throw new StorageException(e.Message, e);
			}
		}
	}
}