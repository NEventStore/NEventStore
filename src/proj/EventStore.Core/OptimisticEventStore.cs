namespace EventStore.Core
{
	using System;
	using System.Collections.Generic;

	public class OptimisticEventStore : IStoreEvents
	{
		private readonly IDictionary<Guid, long> versions = new Dictionary<Guid, long>();
		private readonly IStorageEngine storage;

		public OptimisticEventStore(IStorageEngine storage)
		{
			this.storage = storage;
		}

		public CommittedEventStream Read(Guid id)
		{
			var committed = this.storage.LoadById(id);
			this.versions[committed.Id] = committed.Version;
			return committed;
		}

		public void Write(UncommittedEventStream stream)
		{
			if (!CanWrite(stream))
				return;

			try
			{
				var initialVersion = this.GetVersion(stream.Id);
				this.storage.Save(stream, initialVersion);
				this.versions[stream.Id] = initialVersion + stream.Events.Count;
			}
			catch (DuplicateKeyException e)
			{
				this.WrapAndThrow(stream, e);
			}
		}

		private static bool CanWrite(UncommittedEventStream stream)
		{
			if (stream == null)
				return false;

			return (stream.Events != null && stream.Events.Count > 0) || stream.Snapshot != null;
		}
		private long GetVersion(Guid id)
		{
			long initialVersion;
			if (!this.versions.TryGetValue(id, out initialVersion))
				this.versions[id] = initialVersion;

			return initialVersion;
		}

		private void WrapAndThrow(UncommittedEventStream stream, Exception innerException)
		{
			var events = this.storage.LoadStartingAfter(stream.Id, this.GetVersion(stream.Id));
			if (events.Count > 0)
				throw new ConcurrencyException(ExceptionMessages.Concurrency, innerException, events);

			events = this.storage.LoadByCommandId(stream.CommandId);
			throw new DuplicateCommandException(
				ExceptionMessages.AlreadyHandledCommand, innerException, events);
		}
	}
}