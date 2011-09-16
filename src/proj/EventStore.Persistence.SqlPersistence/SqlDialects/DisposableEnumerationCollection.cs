namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public class DisposableEnumerationCollection<T> : IEnumerable<T>, IEnumerator<T>
	{
		private readonly ICollection<IDisposable> resources = new IDisposable[] { };
		private readonly IEnumerator<T> enumerator;
		private bool disposed;

		public DisposableEnumerationCollection(IEnumerable<T> enumerable, params IDisposable[] resources)
		{
			this.enumerator = enumerable.GetEnumerator();
			this.resources = resources ?? this.resources;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this.disposed)
				return;

			this.disposed = true;

			if (this.enumerator != null)
				this.enumerator.Dispose();

			foreach (var resource in this.resources)
				resource.Dispose();
		}

		public virtual IEnumerator<T> GetEnumerator()
		{
			return this;
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this;
		}

		public virtual bool MoveNext()
		{
			if (this.disposed)
				return false;

			if (this.enumerator == null)
				return false;

			if (this.enumerator.MoveNext())
				return true;

			return false;
		}
		public virtual void Reset()
		{
		}

		public virtual T Current
		{
			get
			{
				if (this.disposed || this.enumerator == null)
					return default(T);

				return this.enumerator.Current;
			}
		}
		object IEnumerator.Current
		{
			get { return this.Current; }
		}
	}
}