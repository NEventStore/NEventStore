namespace NEventStore
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    internal class ImmutableCollection<T> : ICollection<T>, ICollection
    {
        private readonly ICollection<T> _inner;
        private readonly object _lock = new object();

        public ImmutableCollection(ICollection<T> inner)
        {
            _inner = inner;
        }

        public virtual object SyncRoot
        {
            get { return _lock; }
        }

        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        public virtual void CopyTo(Array array, int index)
        {
            CopyTo(array.Cast<T>().ToArray(), index);
        }

        public virtual int Count
        {
            get { return _inner.Count; }
        }

        public virtual bool IsReadOnly
        {
            get { return true; }
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void Add(T item)
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        public virtual bool Remove(T item)
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        public virtual void Clear()
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        public virtual bool Contains(T item)
        {
            return _inner.Contains(item);
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }
    }
}