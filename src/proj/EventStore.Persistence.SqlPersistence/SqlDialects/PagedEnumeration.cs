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
		private int currentIndex;

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
				this.reader = null;

			this.currentPage = 0;
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
			this.currentIndex++;

			if (this.reader.Read())
				return true;

			if (!this.PagingEnabled())
				return false;

			if (!this.PageCompletelyEnumerated())
				return false;

			this.reader.Dispose();
			this.reader = this.OpenNextPage();
			return this.reader.Read();
		}
		private bool PagingEnabled()
		{
			return this.pageSize > 0;
		}
		private bool PageCompletelyEnumerated()
		{
			return this.pageSize == this.currentIndex;
		}
		private IDataReader OpenNextPage()
		{
			try
			{
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