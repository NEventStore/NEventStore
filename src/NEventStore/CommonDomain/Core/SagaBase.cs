namespace CommonDomain.Core
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public class SagaBase<TMessage> : ISaga, IEquatable<ISaga>
		where TMessage : class
	{
		private readonly IDictionary<Type, Action<TMessage>> handlers = new Dictionary<Type, Action<TMessage>>();

		private readonly ICollection<TMessage> uncommitted = new LinkedList<TMessage>();

		private readonly ICollection<TMessage> undispatched = new LinkedList<TMessage>();

		public virtual bool Equals(ISaga other)
		{
			return null != other && other.Id == Id;
		}

		public string Id { get; protected set; }

		public int Version { get; private set; }

		public void Transition(object message)
		{
			this.handlers[message.GetType()](message as TMessage);
			this.uncommitted.Add(message as TMessage);
			this.Version++;
		}

		ICollection ISaga.GetUncommittedEvents()
		{
			return this.uncommitted as ICollection;
		}

		void ISaga.ClearUncommittedEvents()
		{
			this.uncommitted.Clear();
		}

		ICollection ISaga.GetUndispatchedMessages()
		{
			return this.undispatched as ICollection;
		}

		void ISaga.ClearUndispatchedMessages()
		{
			this.undispatched.Clear();
		}

		protected void Register<TRegisteredMessage>(Action<TRegisteredMessage> handler)
			where TRegisteredMessage : class, TMessage
		{
			this.handlers[typeof(TRegisteredMessage)] = message => handler(message as TRegisteredMessage);
		}

		protected void Dispatch(TMessage message)
		{
			this.undispatched.Add(message);
		}

		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as ISaga);
		}
	}
}