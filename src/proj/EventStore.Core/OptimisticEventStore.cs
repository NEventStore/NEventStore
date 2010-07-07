namespace EventStore.Core
{
	using System;

	public class OptimisticEventStore : IStoreEvents
	{
		private readonly IAdaptStorage storage;

		public OptimisticEventStore(IAdaptStorage storage)
		{
			this.storage = storage;
		}

		public CommittedEventStream Read(Guid id, long maxStartingVersion)
		{
			return this.storage.LoadById(id, maxStartingVersion);
		}

		public void Write(UncommittedEventStream stream)
		{
			if (!CanWrite(stream))
				throw new ArgumentException(ExceptionMessages.NoWork, "stream");

			try
			{
				this.storage.Save(stream);
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
		private void WrapAndThrow(UncommittedEventStream stream, Exception innerException)
		{
			var events = this.storage.LoadStartingAfter(stream.Id, stream.ExpectedVersion);
			if (events.Count > 0)
				throw new ConcurrencyException(ExceptionMessages.Concurrency, innerException, events);

			events = this.storage.LoadByCommandId(stream.CommandId);
			throw new DuplicateCommandException(ExceptionMessages.Duplicate, innerException, events);
		}
	}
}