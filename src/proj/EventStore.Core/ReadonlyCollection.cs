namespace EventStore
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	internal class ReadonlyCollection<T> : ICollection<T>
	{
		private readonly ICollection<T> inner;

		public ReadonlyCollection(ICollection<T> inner)
		{
			this.inner = inner;
		}

		public int Count
		{
			get { return this.inner.Count; }
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
	}
}