namespace CommonDomain.Persistence.EventStore
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using NEventStore;
	using NEventStore.Persistence;

	public class SagaEventStoreRepository : ISagaRepository, IDisposable
	{
		private const string SagaTypeHeader = "SagaType";

		private const string UndispatchedMessageHeader = "UndispatchedMessage.";

		private readonly IStoreEvents eventStore;

		private readonly IDictionary<Guid, IEventStream> streams = new Dictionary<Guid, IEventStream>();

		public SagaEventStoreRepository(IStoreEvents eventStore)
		{
			this.eventStore = eventStore;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public TSaga GetById<TSaga>(Guid sagaId) where TSaga : class, ISaga, new()
		{
			return BuildSaga<TSaga>(this.OpenStream(sagaId));
		}

		public void Save(ISaga saga, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
		{
			if (saga == null)
			{
				throw new ArgumentNullException("saga", ExceptionMessages.NullArgument);
			}

			Dictionary<string, object> headers = PrepareHeaders(saga, updateHeaders);
			IEventStream stream = this.PrepareStream(saga, headers);

			Persist(stream, commitId);

			saga.ClearUncommittedEvents();
			saga.ClearUndispatchedMessages();
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

				this.streams.Clear();
			}
		}

		private IEventStream OpenStream(Guid sagaId)
		{
			IEventStream stream;
			if (this.streams.TryGetValue(sagaId, out stream))
			{
				return stream;
			}

			try
			{
				stream = this.eventStore.OpenStream(sagaId, 0, int.MaxValue);
			}
			catch (StreamNotFoundException)
			{
				stream = this.eventStore.CreateStream(sagaId);
			}

			return this.streams[sagaId] = stream;
		}

		private static TSaga BuildSaga<TSaga>(IEventStream stream) where TSaga : class, ISaga, new()
		{
			var saga = new TSaga();
			foreach (var @event in stream.CommittedEvents.Select(x => x.Body))
			{
				saga.Transition(@event);
			}

			saga.ClearUncommittedEvents();
			saga.ClearUndispatchedMessages();

			return saga;
		}

		private static Dictionary<string, object> PrepareHeaders(
			ISaga saga, Action<IDictionary<string, object>> updateHeaders)
		{
			var headers = new Dictionary<string, object>();

			headers[SagaTypeHeader] = saga.GetType().FullName;
			if (updateHeaders != null)
			{
				updateHeaders(headers);
			}

			int i = 0;
			foreach (var command in saga.GetUndispatchedMessages())
			{
				headers[UndispatchedMessageHeader + i++] = command;
			}

			return headers;
		}

		private IEventStream PrepareStream(ISaga saga, Dictionary<string, object> headers)
		{
			IEventStream stream;
			if (!this.streams.TryGetValue(saga.Id, out stream))
			{
				this.streams[saga.Id] = stream = this.eventStore.CreateStream(saga.Id);
			}

			foreach (var item in headers)
			{
				stream.UncommittedHeaders[item.Key] = item.Value;
			}

			saga.GetUncommittedEvents().Cast<object>().Select(x => new EventMessage { Body = x }).ToList().ForEach(stream.Add);

			return stream;
		}

		private static void Persist(IEventStream stream, Guid commitId)
		{
			try
			{
				stream.CommitChanges(commitId);
			}
			catch (DuplicateCommitException)
			{
				stream.ClearChanges();
			}
			catch (StorageException e)
			{
				throw new PersistenceException(e.Message, e);
			}
		}
	}
}