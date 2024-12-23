using System.Collections;

namespace NEventStore
{
    /// <summary>
    ///    Represents an immutable dictionary.
    /// </summary>
    public sealed class ImmutableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _inner;

        /// <summary>
        /// Initializes a new instance of the ImmutableDictionary class.
        /// </summary>
        public ImmutableDictionary(IDictionary<TKey, TValue> inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public TValue this[TKey key] { get => _inner[key]; set => throw new NotSupportedException(Resources.ReadOnlyCollection); }

        /// <summary>
        /// Gets the keys in the dictionary.
        /// </summary>
        public ICollection<TKey> Keys => _inner.Keys;

        /// <summary>
        /// Gets the values in the dictionary.
        /// </summary>
        public ICollection<TValue> Values => _inner.Values;

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        public int Count => _inner.Count;

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        public bool IsReadOnly => true;

        /// <summary>
        /// Adds an element with the provided key and value to the dictionary.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public void Add(TKey key, TValue value)
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        /// <summary>
        /// Adds an item to the dictionary.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        /// <summary>
        /// Removes all items from the dictionary.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public void Clear()
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        /// <summary>
        /// Determines whether the dictionary contains a specific value.
        /// </summary>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _inner.Contains(item);
        }

        /// <summary>
        /// Determines whether the dictionary contains a specific key.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            return _inner.ContainsKey(key);
        }

        /// <summary>
        /// Copies the elements of the dictionary to an array, starting at a particular array index.
        /// </summary>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the element with the specified key from the dictionary.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public bool Remove(TKey key)
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the dictionary.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(Resources.ReadOnlyCollection);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _inner.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}