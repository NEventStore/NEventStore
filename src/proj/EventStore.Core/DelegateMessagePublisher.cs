namespace EventStore.Core
{
	using System;
	using Dispatcher;
	using Persistence;

	public class DelegateMessagePublisher : IPublishMessages
	{
		private readonly Action<Commit> publish;

		public DelegateMessagePublisher(Action<Commit> publish)
		{
			this.publish = publish;
		}

		public virtual void Publish(Commit commit)
		{
			this.publish(commit);
		}
	}
}