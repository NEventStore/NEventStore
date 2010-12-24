namespace EventStore.Dispatcher
{
	using System;
	using Persistence;

	public class DelegateMessagePublisher : IPublishMessages
	{
		private readonly Action<Commit> publish;

		public DelegateMessagePublisher(Action<Commit> publish)
		{
			this.publish = publish;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			// no op
		}

		public virtual void Publish(Commit commit)
		{
			this.publish(commit);
		}
	}
}