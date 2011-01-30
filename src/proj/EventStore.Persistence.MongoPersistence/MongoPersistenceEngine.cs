namespace EventStore.Persistence.MongoPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Norm;
	using Norm.Collections;
	using Norm.Protocol.Messages;
	using Serialization;

	public class MongoPersistenceEngine : IPersistStreams
	{
		private const string ConcurrencyException = "E1100";
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

		private IMongoCollection<MongoCommit> PersistedCommits
		{
			get { return this.store.Database.GetCollection<MongoCommit>(); }
		}
		private IMongoCollection<MongoSnapshot> PersistedSnapshots
		{
			get { return this.store.Database.GetCollection<MongoSnapshot>(); }
		}

		public virtual void Initialize()
		{
			this.PersistedCommits.CreateIndex(
				x => x.Dispatched,
				"Dispatched_Index",
				false,
				IndexOption.Ascending);

			this.PersistedCommits.CreateIndex(
				x => new { x.StreamId, x.MinStreamRevision, x.MaxStreamRevision },
				"GetFrom_Index",
				true,
				IndexOption.Ascending);

			this.PersistedSnapshots.CreateIndex(
				x => new { x.StreamId, x.StreamRevision },
				"GetSnapshot",
				true,
				IndexOption.Ascending);
		}

		public virtual IEnumerable<Commit> GetFrom(Guid streamId, int minRevision, int maxRevision)
		{
			try
			{
				return this.PersistedCommits.AsQueryable()
					.Where(x => x.StreamId == streamId
						&& x.MaxStreamRevision >= minRevision
						&& x.MinStreamRevision <= maxRevision).ToArray()
					.Select(x => x.ToCommit(this.serializer));
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
				return this.PersistedCommits.AsQueryable()
					.Where(x => x.CommitStamp >= start).ToArray()
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
				// TODO: update associated StreamHead--should be done asynchronously.
				this.PersistedCommits.Insert(commit);
			}
			catch (MongoException e)
			{
				if (!e.Message.StartsWith(ConcurrencyException))
					throw new StorageException(e.Message, e);

				var committed = this.PersistedCommits.FindOne(commit.ToMongoExpando());
				if (committed != null && committed.CommitId != commit.CommitId)
					throw new ConcurrencyException();

				throw new DuplicateCommitException();
			}
		}

		public virtual IEnumerable<Commit> GetUndispatchedCommits()
		{
			return this.PersistedCommits.AsQueryable()
				.Where(x => !x.Dispatched).ToArray()
				.Select(x => x.ToCommit(this.serializer));
		}
		public virtual void MarkCommitAsDispatched(Commit commit)
		{
			var expando = commit.ToMongoCommit(this.serializer).ToMongoExpando();
			this.PersistedCommits.Update(expando, x => x.SetValue(mc => mc.Dispatched, true));
		}

		public virtual IEnumerable<StreamHead> GetStreamsToSnapshot(int maxThreshold)
		{
			return new StreamHead[0]; // TODO
		}
		public virtual Snapshot GetSnapshot(Guid streamId, int maxRevision)
		{
			return this.PersistedSnapshots.AsQueryable()
				.Where(x => x.StreamId == streamId && x.StreamRevision <= maxRevision)
				.OrderByDescending(x => x.StreamRevision)
				.FirstOrDefault().ToSnapshot(this.serializer);
		}
		public virtual bool AddSnapshot(Snapshot snapshot)
		{
			if (snapshot == null)
				return false;

			try
			{
				// TODO: update associated StreamHead--should be done asynchronously.
				this.PersistedSnapshots.Insert(snapshot.ToMongoSnapshot(this.serializer));
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