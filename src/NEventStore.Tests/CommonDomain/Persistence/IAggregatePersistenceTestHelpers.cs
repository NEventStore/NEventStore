namespace CommonDomain
{
	using System;
	using System.Reflection;

	using CommonDomain.Persistence;

	internal class AggregateFactory : IConstructAggregates
	{
		public IAggregate Build(Type type, Guid id, IMemento snapshot)
		{
			ConstructorInfo constructor = type.GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Guid) }, null);

			return constructor.Invoke(new object[] { id }) as IAggregate;
		}
	}
}