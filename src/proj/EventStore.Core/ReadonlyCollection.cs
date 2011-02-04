namespace EventStore
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	internal class ReadOnlyCollection<T> : ICollection<T>, ICollection
	{
		private readonly object @lock = new object();
		private readonly ICollection<T> inner;

		public ReadOnlyCollection(ICollection<T> inner)
		{
			this.inner = inner;
		}

		public int Count
		{
			get { return this.inner.Count; }
		}
		public object SyncRoot
		{
			get { return this.@lock; }
		}
		public bool IsSynchronized
		{
			get { return false; }
		}
		public bool IsReadOnly
		{
			get { return true; }
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this.inner.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public void Add(T item)
		{
			throw new NotSupportedException(Resources.ReadonlyCollection);
		}
		public bool Remove(T item)
		{
			throw new NotSupportedException(Resources.ReadonlyCollection);
		}
		public void Clear()
		{
			throw new NotSupportedException(Resources.ReadonlyCollection);
		}

		public bool Contains(T item)
		{
			return this.inner.Contains(item);
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			this.inner.CopyTo(array, arrayIndex);
		}
		public void CopyTo(Array array, int index)
		{
			this.CopyTo(array.Cast<T>().ToArray(), index);
		}
	}
}