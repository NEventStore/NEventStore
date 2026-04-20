using System.Collections;

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

        public bool IsSynchronized
        {
            get { return false; }
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException("Only single dimensional arrays are supported.", nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException("Arrays with non-zero lower bounds are not supported.", nameof(array));
            }

            if ((uint)index > (uint)array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (array.Length - index < _inner.Count)
            {
                throw new ArgumentException("The destination array does not have enough space.", nameof(array));
            }

            if (array is T[] typedArray)
            {
                // Calls through the non-generic ICollection interface can still pass a T[].
                // Keep those on the typed CopyTo path instead of using Array.SetValue per item.
                _inner.CopyTo(typedArray, index);
                return;
            }

            // ICollection.CopyTo accepts non-generic arrays such as object[]. The previous
            // implementation used Cast<T>().ToArray(), which allocated a temporary array and
            // populated that temporary instead of the caller-provided destination. SetValue keeps
            // the compatibility surface while copying directly into the requested array.
            foreach (var item in _inner)
            {
                array.SetValue(item, index++);
            }
        }

        public int Count
        {
            get { return _inner.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

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
