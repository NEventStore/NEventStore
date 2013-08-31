namespace NEventStore
{
    using System.Collections;
    using System.Collections.Generic;

    internal class EnumerableCounter : IEnumerable<Commit>
    {
        private readonly IEnumerable<Commit> enumerable;

        public EnumerableCounter(IEnumerable<Commit> enumerable)
        {
            this.enumerable = enumerable;
            GetEnumeratorCallCount = 0;
        }

        public int GetEnumeratorCallCount { get; private set; }

        public IEnumerator<Commit> GetEnumerator()
        {
            GetEnumeratorCallCount++;
            return enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}