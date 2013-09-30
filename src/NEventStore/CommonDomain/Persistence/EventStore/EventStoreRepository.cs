namespace CommonDomain.Persistence.EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using NEventStore;
	using NEventStore.Persistence;

	public class EventStoreRepository : IRepository, IDisposable
	{
		private const string AggregateTypeHeader = "AggregateType";

		private readonly IDetectConflicts conflictDetector;

		private readonly IStoreEvents eventStore;

		private readonly IConstructAggregates factory;

		private readonly IDictionary<string, ISnapshot> snapshots = new Dictionary<string, ISnapshot>();

		private readonly IDictionary<string, IEventStream> streams = new Dictionary<string, IEventStream>();

		public EventStoreRepository(IStoreEvents eventStore, IConstructAggregates factory, IDetectConflicts conflictDetector)
		{
			this.eventStore = eventStore;
			this.factory = factory;
			this.conflictDetector = conflictDetector;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public virtual TAggregate GetById<TAggregate>(Guid id) where TAggregate : class, IAggregate
		{
			return this.GetById<TAggregate>(Bucket.Default, id);
		}

		public virtual TAggregate GetById<TAggregate>(Guid id, int versionToLoad) where TAggregate : class, IAggregate
		{
			return this.GetById<TAggregate>(Bucket.Default, id, versionToLoad);
		}

		public TAggregate GetById<TAggregate>(string bucketId, Guid id) where TAggregate : class, IAggregate
		{
			return this.GetById<TAggregate>(bucketId, id, int.MaxValue);
		}

		public TAggregate GetById<TAggregate>(string bucketId, Guid id, int versionToLoad) where TAggregate : class, IAggregate
		{
			ISnapshot snapshot = this.GetSnapshot(bucketId, id, versionToLoad);
			IEventStream stream = this.OpenStream(bucketId, id, versionToLoad, snapshot);
			IAggregate aggregate = this.GetAggregate<TAggregate>(snapshot, stream);

			ApplyEventsToAggregate(versionToLoad, stream, aggregate);

			return aggregate as TAggregate;
		}

		public virtual void Save(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
		{
			Save(Bucket.Default, aggregate, commitId, updateHeaders);

		}

		public void Save(string bucketId, IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
		{
			Dictionary<string, object> headers = PrepareHeaders(aggregate, updateHeaders);
			while (true)
			{
				IEventStream stream = this.PrepareStream(bucketId, aggregate, headers);
				int commitEventCount = stream.CommittedEvents.Count;

				try
				{
					stream.CommitChanges(commitId);
					aggregate.ClearUncommittedEvents();
					return;
				}
				catch (DuplicateCommitException)
				{
					stream.ClearChanges();
					return;
				}
				catch (ConcurrencyException e)
				{
					if (this.ThrowOnConflict(stream, commitEventCount))
					{
						throw new ConflictingCommandException(e.Message, e);
					}

					stream.ClearChanges();
				}
				catch (StorageException e)
				{
					throw new PersistenceException(e.Message, e);
				}
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			lock (this.streams)
			{
				foreach (var stream in this.streams)
				{
					stream.Value.Dispose();
				}

				this.snapshots.Clear();
				this.streams.Clear();
			}
		}

		private static void ApplyEventsToAggregate(int versionToLoad, IEventStream stream, IAggregate aggregate)
		{
			if (versionToLoad == 0 || aggregate.Version < versionToLoad)
			{
				foreach (var @event in stream.CommittedEvents.Select(x => x.Body))
				{
					aggregate.ApplyEvent(@event);
				}
			}
		}

		private IAggregate GetAggregate<TAggregate>(ISnapshot snapshot, IEventStream stream)
		{
			IMemento memento = snapshot == null ? null : snapshot.Payload as IMemento;
			return this.factory.Build(typeof(TAggregate), stream.StreamId.ToGuid(), memento);
		}

		private ISnapshot GetSnapshot(string bucketId, Guid id, int version)
		{
			ISnapshot snapshot;
			var snapshotId = bucketId + id;
			if (!this.snapshots.TryGetValue(snapshotId, out snapshot))
			{
				this.snapshots[snapshotId] = snapshot = this.eventStore.Advanced.GetSnapshot(bucketId, id, version);
			}

			return snapshot;
		}

		private IEventStream OpenStream(string bucketId, Guid id, int version, ISnapshot snapshot)
		{
			IEventStream stream;
			var streamsId = bucketId + id;
			if (this.streams.TryGetValue(streamsId, out stream))
			{
				return stream;
			}

			stream = snapshot == null
								 ? this.eventStore.OpenStream(bucketId, id, 0, version)
				         : this.eventStore.OpenStream(snapshot, version);

			return this.streams[streamsId] = stream;
		}

		private IEventStream PrepareStream(string bucketId, IAggregate aggregate, Dictionary<string, object> headers)
		{
			IEventStream stream;
			var streamsId = bucketId + aggregate.Id;
			if (!this.streams.TryGetValue(streamsId, out stream))
			{
				this.streams[streamsId] = stream = this.eventStore.CreateStream(bucketId, aggregate.Id);
			}

			foreach (var item in headers)
			{
				stream.UncommittedHeaders[item.Key] = item.Value;
			}

			aggregate.GetUncommittedEvents()
			         .Cast<object>()
			         .Select(x => new EventMessage { Body = x })
			         .ToList()
			         .ForEach(stream.Add);

			return stream;
		}

		private static Dictionary<string, object> PrepareHeaders(
			IAggregate aggregate, Action<IDictionary<string, object>> updateHeaders)
		{
			var headers = new Dictionary<string, object>();

			headers[AggregateTypeHeader] = aggregate.GetType().FullName;
			if (updateHeaders != null)
			{
				updateHeaders(headers);
			}

			return headers;
		}

		private bool ThrowOnConflict(IEventStream stream, int skip)
		{
			IEnumerable<object> committed = stream.CommittedEvents.Skip(skip).Select(x => x.Body);
			IEnumerable<object> uncommitted = stream.UncommittedEvents.Select(x => x.Body);
			return this.conflictDetector.ConflictsWith(uncommitted, committed);
		}
	}
}