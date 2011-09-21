namespace EventStore.Persistence.RavenPersistence
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Transactions;

	public sealed class PagedEnumerationCollection<T> : IEnumerable<T>
	{
		private readonly IQueryable<T> source;
		private readonly int take;
		private readonly TransactionScope scope;

		public PagedEnumerationCollection(IQueryable<T> source, int take, TransactionScope scope)
		{
			this.source = source;
			this.scope = scope;
			this.take = take;
		}
		public IEnumerator<T> GetEnumerator()
		{
			return new PagedEnumerator(this.source, this.take, this.scope);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		private sealed class PagedEnumerator : IEnumerator<T>
		{
			private readonly IQueryable<T> source;
			private readonly int take;
			private readonly TransactionScope scope;
			private int skip;
			private IEnumerator<T> current;

			public PagedEnumerator(IQueryable<T> source, int take, TransactionScope scope)
			{
				this.source = source;
				this.scope = scope;
				this.take = take;
			}
			public void Dispose()
			{
				this.Reset();

				if (this.scope != null)
				{
					this.scope.Complete();
					this.scope.Dispose();
				}
				
				GC.SuppressFinalize(this);
			}

			public void Reset()
			{
				if (this.current != null)
					this.current.Dispose();

				this.current = null;
				this.skip = 0;
			}
			public bool MoveNext()
			{
				this.current = this.current ?? this.source.Skip(this.skip).Take(this.take).GetEnumerator();

				if (this.current.MoveNext())
					return this.IncrementPosition();

				if (!this.PageCompletelyEnumerated())
					return false; // ISSUE: if our page size doesn't agree with Raven, this won't evaluate properly...

				this.current.Dispose();
				this.current = this.source.Skip(this.skip).Take(this.take).GetEnumerator();

				if (this.current.MoveNext())
					return this.IncrementPosition();

				return false;
			}
			private bool IncrementPosition()
			{
				this.skip++;
				return true;
			}
			private bool PageCompletelyEnumerated()
			{
				return this.skip > 0 && 0 == this.skip % this.take;
			}
			
			public T Current
			{
				get { return this.current == null ? default(T) : this.current.Current; }
			}
			object IEnumerator.Current
			{
				get { return this.Current; }
			}
		}
	}
}