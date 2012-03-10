namespace EventStore
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	internal class ImmutableCollection<T> : ICollection<T>, ICollection
	{
		private readonly object @lock = new object();
		private readonly ICollection<T> inner;

		public ImmutableCollection(ICollection<T> inner)
		{
			this.inner = inner;
		}

		public virtual int Count
		{
			get { return this.inner.Count; }
		}
		public virtual object SyncRoot
		{
			get { return this.@lock; }
		}
		public virtual bool IsSynchronized
		{
			get { return false; }
		}
		public virtual bool IsReadOnly
		{
			get { return true; }
		}

		public virtual IEnumerator<T> GetEnumerator()
		{
			return this.inner.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
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
			return this.inner.Contains(item);
		}
		public virtual void CopyTo(T[] array, int arrayIndex)
		{
			this.inner.CopyTo(array, arrayIndex);
		}
		public virtual void CopyTo(Array array, int index)
		{
			this.CopyTo(array.Cast<T>().ToArray(), index);
		}
	}
}