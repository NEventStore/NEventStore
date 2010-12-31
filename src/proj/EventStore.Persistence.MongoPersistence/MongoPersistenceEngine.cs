namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Norm;
	using Norm.BSON;
	using Norm.Configuration;
	using Norm.Protocol.Messages;
	using Serialization;

	public class MongoPersistenceEngine : IPersistStreams
	{
		private readonly IMongo store;
		private readonly ISerialize serializer;
		private bool disposed;

		public MongoPersistenceEngine(IMongo store, ISerialize serializer)
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
			this.store.Dispose();
		}

		public virtual void Initialize()
		{
			MongoConfiguration.Initialize(c => c.For<StreamHead>(stream => stream.IdIs(i => i.StreamId)));

			this.store.Database.GetCollection<MongoCommit>().CreateIndex(
				mc => mc.Dispatched, "Dispatched_Index", false, IndexOption.Ascending);

			this.store.Database.GetCollection<MongoCommit>().CreateIndex(
				mc => new { mc.StreamId, mc.StreamRevision }, "GetFrom_Index", false, IndexOption.Ascending);
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, long minRevision)
		{
			try
			{
				var collection = this.store.Database.GetCollection<MongoCommit>();
				return collection.AsQueryable()
					.Where(x => x.StreamId == streamId && x.StreamRevision >= minRevision).ToArray()
					.Select(mc => mc.ToCommit(this.serializer));
			}
			catch (Exception e)
			{
				throw new PersistenceEngineException(e.Message, e);
			}
		}
		public virtual IEnumerable<Commit> GetUntil(Guid streamId, long maxRevision)
		{
			var snapshotCommit = this.store.Database.GetCollection<MongoCommit>().AsQueryable()
				.Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision && x.Snapshot != null)
				.OrderByDescending(o => o.StreamRevision)
				.Take(1)
				.FirstOrDefault();

			long snapshotRevision = 0;
			if (snapshotCommit != null)
				snapshotRevision = snapshotCommit.StreamRevision;

			return this.store.Database.GetCollection<MongoCommit>().AsQueryable()
				.Where(x => x.StreamId == streamId && x.StreamRevision >= snapshotRevision && x.StreamRevision <= maxRevision)
				.ToArray()
				.Select(x => x.ToCommit(this.serializer));
		}
		public virtual void Persist(CommitAttempt uncommitted)
		{
			var commit = uncommitted.ToMongoCommit(this.serializer);

			try
			{
				this.store.Database.GetCollection<MongoCommit>().Insert(commit);
			}
			catch (MongoException mongoException)
			{
				if (!mongoException.Message.StartsWith("E11000"))
					throw;

				var committed = this.store.Database.GetCollection<MongoCommit>().FindOne(commit);
				if (committed == null)
					throw new ConcurrencyException();

				throw new DuplicateCommitException();
			}

			this.store.Database.GetCollection<StreamHead>()
				.Save(new StreamHead(commit.StreamId, null, commit.StreamRevision, 0));
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.store.Database.GetCollection<MongoCommit>().AsQueryable()
				.Where(x => !x.Dispatched).ToArray()
				.Select(x => x.ToCommit(this.serializer));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			this.store.Database.GetCollection<MongoCommit>()
				.Update(commit.ToMongoCommit(this.serializer).ToMongoExpando(), u => u.SetValue(mc => mc.Dispatched, true));
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return this.store.Database.GetCollection<StreamHead>().AsQueryable()
				.Where(x => x.HeadRevision >= x.SnapshotRevision + maxThreshold).ToArray()
				.Select(stream => new StreamHead(stream.StreamId, stream.StreamName, stream.HeadRevision, stream.SnapshotRevision));
		}
		public virtual void AddSnapshot(Guid streamId, long streamRevision, object snapshot)
		{
			var commit = new Expando();

			commit["StreamId"] = streamId;
			commit["StreamRevision"] = streamRevision;

			this.store.Database.GetCollection<MongoCommit>()
				.Update(commit, u => u.SetValue(mc => mc.Snapshot, this.serializer.Serialize(snapshot)));

			var stream = new StreamHead(streamId, null, streamRevision, 0);
			this.store.Database.GetCollection<StreamHead>()
				.Update(stream.ToMongoExpando(), u => u.SetValue(s => s.SnapshotRevision, streamRevision));
		}
	}
}