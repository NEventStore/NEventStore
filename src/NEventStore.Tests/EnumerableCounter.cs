namespace NEventStore
{
    using System.Collections;
    using System.Collections.Generic;

    internal class EnumerableCounter : IEnumerable<Commit>
    {
        private readonly IEnumerable<Commit> enumerable;
        public int GetEnumeratorCallCount { get; private set; }

        public EnumerableCounter(IEnumerable<Commit> enumerable)
        {
            this.enumerable = enumerable;
            this.GetEnumeratorCallCount = 0;
        }
        public IEnumerator<Commit> GetEnumerator()
        {
            this.GetEnumeratorCallCount++;
            return this.enumerable.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}