using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NEventStore
{
    internal sealed class ImmutableCollection<T> : ICollection<T>, ICollection
    {
        private readonly ICollection<T> _inner;

        public ImmutableCollection(ICollection<T> inner)
        {
            _inner = inner;
        }

        public object SyncRoot { get; } = new object();

        public bool IsSynchronized => false;

        public void CopyTo(Array array, int index)
        {
            CopyTo(array.Cast<T>().ToArray(), index);
        }

        public int Count => _inner.Count;

        public bool IsReadOnly => true;

        public IEnumerator<T> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        public void Clear()
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        public bool Contains(T item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }
    }
}