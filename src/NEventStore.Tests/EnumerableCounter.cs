namespace NEventStore
{
    using System.Collections;
    using System.Collections.Generic;

    internal class EnumerableCounter<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _enumerable;

        public EnumerableCounter(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
            GetEnumeratorCallCount = 0;
        }

        public int GetEnumeratorCallCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            GetEnumeratorCallCount++;
            return _enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}