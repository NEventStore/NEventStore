namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;

	public class PagedEnumeration : IEnumerable<IDataRecord>, IEnumerator<IDataRecord>
	{
		private readonly IDbCommand command;
		private readonly IDataParameter skip;
		private readonly int pageSize;
		private IDataReader reader;
		private int currentPage;
		private int position;

		public PagedEnumeration(IDbCommand command, IDataParameter skip, int pageSize)
		{
			this.command = command;
			this.skip = skip;
			this.pageSize = pageSize;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && this.reader != null)
				this.reader.Dispose();

			this.reader = null;
			this.currentPage = this.position = 0;
		}

		public virtual IEnumerator<IDataRecord> GetEnumerator()
		{
			return this;
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this;
		}

		bool IEnumerator.MoveNext()
		{
			this.reader = this.reader ?? this.OpenNextPage();

			if (this.reader.Read())
				return this.IncrementPosition();

			if (!this.PagingEnabled())
				return false;

			if (!this.PageCompletelyEnumerated())
				return false;

			this.reader.Dispose();
			this.reader = this.OpenNextPage();

			if (reader.Read())
				return this.IncrementPosition();

			return false;
		}
		private bool IncrementPosition()
		{
			return ++this.position > 0;
		}

		private bool PagingEnabled()
		{
			return this.pageSize > 0;
		}
		private bool PageCompletelyEnumerated()
		{
			return this.position > 0 && 0 == this.position % this.pageSize;
		}
		private IDataReader OpenNextPage()
		{
			try
			{
				if (this.skip != null)
					this.skip.Value = this.pageSize * this.currentPage++;

				return this.command.ExecuteReader();
			}
			catch (Exception e)
			{
				throw new StorageException(e.Message, e);
			}
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException("Forward-only readers.");
		}
		IDataRecord IEnumerator<IDataRecord>.Current
		{
			get { return this.reader; }
		}
		object IEnumerator.Current
		{
			get { return ((IEnumerator<IDataRecord>)this).Current; }
		}
	}
}