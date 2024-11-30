#region

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace NEventStore;

public sealed class ImmutableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
    private readonly IDictionary<TKey, TValue> _inner;

    public ImmutableDictionary(IDictionary<TKey, TValue> inner)
    {
        _inner = inner;
    }

    public TValue this[TKey key]
    {
        get => _inner[key];
        set => throw new NotSupportedException(Resources.ReadOnlyCollection);
    }

    public ICollection<TKey> Keys => _inner.Keys;

    public ICollection<TValue> Values => _inner.Values;

    public int Count => _inner.Count;

    public bool IsReadOnly => true;

    public void Add(TKey key, TValue value)
    {
        throw new NotSupportedException(Resources.ReadOnlyCollection);
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        throw new NotSupportedException(Resources.ReadOnlyCollection);
    }

    public void Clear()
    {
        throw new NotSupportedException(Resources.ReadOnlyCollection);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return _inner.Contains(item);
    }

    public bool ContainsKey(TKey key)
    {
        return _inner.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        _inner.CopyTo(array, arrayIndex);
    }

    public bool Remove(TKey key)
    {
        throw new NotSupportedException(Resources.ReadOnlyCollection);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        throw new NotSupportedException(Resources.ReadOnlyCollection);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return _inner.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _inner.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}