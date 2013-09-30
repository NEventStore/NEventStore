namespace CommonDomain
{
	using System;

	using global::CommonDomain.Core;

	internal class TestAggregate : AggregateBase
	{
		private TestAggregate(Guid id)
		{
			this.Id = id;
		}

		public TestAggregate(Guid id, string name)
			: this(id)
		{
			this.RaiseEvent(new TestAggregateCreatedEvent { Id = this.Id, Name = name });
		}

		public string Name { get; set; }

		public void ChangeName(string newName)
		{
			this.RaiseEvent(new NameChangedEvent { Name = newName });
		}

		private void Apply(TestAggregateCreatedEvent @event)
		{
			this.Name = @event.Name;
		}

		private void Apply(NameChangedEvent @event)
		{
			this.Name = @event.Name;
		}
	}

	public interface IDomainEvent
	{}

	[Serializable]
	public class NameChangedEvent : IDomainEvent
	{
		public string Name { get; set; }
	}

	[Serializable]
	public class TestAggregateCreatedEvent : IDomainEvent
	{
		public Guid Id { get; set; }

		public string Name { get; set; }
	}
}