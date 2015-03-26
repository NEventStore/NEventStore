namespace CommonDomain.Persistence
{
    using System;
    using System.Reflection;

    public class AggregateFactory : IConstructAggregates
    {
        public IAggregate Build(Type type, Guid id, IMemento snapshot)
        {
            Type typeParam = snapshot != null ? snapshot.GetType() : typeof(Guid);
            object[] paramArray;
            if (snapshot != null)
                paramArray = new object[] { snapshot };
            else
                paramArray = new object[] { id };

            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeParam }, null);

            if (constructor == null)
            {
                throw new InvalidOperationException(
                    string.Format("Aggregate {0} cannot be created: constructor with proper parameter not provided",
                        type.Name));
            }
            return constructor.Invoke(paramArray) as IAggregate;
        }
    }
}