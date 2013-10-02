namespace CommonDomain.Core
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public abstract class AggregateBase : IAggregate, IEquatable<IAggregate>
	{
		private readonly ICollection<object> uncommittedEvents = new LinkedList<object>();

		private IRouteEvents registeredRoutes;

		protected AggregateBase()
			: this(null)
		{}

		protected AggregateBase(IRouteEvents handler)
		{
			if (handler == null)
			{
				return;
			}

			this.RegisteredRoutes = handler;
			this.RegisteredRoutes.Register(this);
		}

		protected IRouteEvents RegisteredRoutes
		{
			get
			{
				return this.registeredRoutes ?? (this.registeredRoutes = new ConventionEventRouter(true, this));
			}
			set
			{
				if (value == null)
				{
					throw new InvalidOperationException("AggregateBase must have an event router to function");
				}

				this.registeredRoutes = value;
			}
		}

		public Guid Id { get; protected set; }

		public int Version { get; protected set; }

		void IAggregate.ApplyEvent(object @event)
		{
			this.RegisteredRoutes.Dispatch(@event);
			this.Version++;
		}

		ICollection IAggregate.GetUncommittedEvents()
		{
			return (ICollection)this.uncommittedEvents;
		}

		void IAggregate.ClearUncommittedEvents()
		{
			this.uncommittedEvents.Clear();
		}

		IMemento IAggregate.GetSnapshot()
		{
			IMemento snapshot = this.GetSnapshot();
			snapshot.Id = this.Id;
			snapshot.Version = this.Version;
			return snapshot;
		}

		public virtual bool Equals(IAggregate other)
		{
			return null != other && other.Id == this.Id;
		}

		protected void Register<T>(Action<T> route)
		{
			this.RegisteredRoutes.Register(route);
		}

		protected void RaiseEvent(object @event)
		{
			((IAggregate)this).ApplyEvent(@event);
			this.uncommittedEvents.Add(@event);
		}

		protected virtual IMemento GetSnapshot()
		{
			return null;
		}

		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as IAggregate);
		}
	}
}