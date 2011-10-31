namespace EventStore.Persistence.SqlPersistence.SqlDialects
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Transactions;
	using Logging;

	public class PagedEnumerationCollection<T> : IEnumerable<T>, IEnumerator<T>
	{
		private readonly ILog logger = LogFactory.BuildLogger(typeof(PagedEnumerationCollection<T>));
		private readonly IEnumerable<IDisposable> disposable = new IDisposable[] { };
		private readonly IDbCommand command;
		private readonly Func<IDataRecord, T> select;
		private readonly NextPageDelegate<T> onNextPage;
		private readonly int pageSize;
		private readonly TransactionScope scope;
		private IDataReader reader;
		private int position;
		private T latest;
		private bool disposed;

		public PagedEnumerationCollection(
			IDbCommand command,
			Func<IDataRecord, T> select,
			NextPageDelegate<T> onNextPage,
			int pageSize,
			TransactionScope scope,
			params IDisposable[] disposable)
		{
			this.command = command;
			this.select = select;
			this.onNextPage = onNextPage;
			this.pageSize = pageSize;
			this.scope = scope;
			this.disposable = disposable ?? this.disposable;
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
			this.position = 0;
			this.latest = default(T);

			if (this.reader != null)
				this.reader.Dispose();
			
			this.reader = null;

			if (this.command != null)
				this.command.Dispose();

			// queries do not modify state and thus calling Complete() on a so-called 'failed' query only
			// allows any outer transaction scope to decide the fate of the transaction
			if (this.scope != null)
				this.scope.Complete(); // caller will dispose scope.

			foreach (var dispose in this.disposable)
				dispose.Dispose();
		}

		public virtual IEnumerator<T> GetEnumerator()
		{
			if (this.disposed)
				throw new ObjectDisposedException(Messages.ObjectAlreadyDisposed);

			return this;
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		bool IEnumerator.MoveNext()
		{
			if (this.disposed)
				throw new ObjectDisposedException(Messages.ObjectAlreadyDisposed);

			if (this.MoveToNextRecord())
				return true;

			this.logger.Verbose(Messages.QueryCompleted);
			return false;
		}
		private bool MoveToNextRecord()
		{
			this.reader = this.reader ?? this.OpenNextPage();

			if (this.reader.Read())
				return this.IncrementPosition();

			if (!this.PagingEnabled())
				return false;

			if (!this.PageCompletelyEnumerated())
				return false;

			this.logger.Verbose(Messages.EnumeratedRowCount, this.position);
			this.reader.Dispose();
			this.reader = this.OpenNextPage();

			if (this.reader.Read())
				return this.IncrementPosition();

			return false;
		}

		private bool IncrementPosition()
		{
			this.position++;
			return true;
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
			if (this.pageSize > 0 && this.position >= this.pageSize)
				this.onNextPage(this.command, this.latest);

			try
			{
				return this.command.ExecuteReader();
			}
			catch (Exception e)
			{
				this.logger.Debug(Messages.EnumerationThrewException, e.GetType());
				throw new StorageUnavailableException(e.Message, e);
			}
		}

		public virtual void Reset()
		{
			throw new NotSupportedException("Forward-only readers.");
		}
		public virtual T Current
		{
			get
			{
				if (this.disposed)
					throw new ObjectDisposedException(Messages.ObjectAlreadyDisposed);

				if (this.reader == null)
					return default(T);

				return this.latest = this.select(this.reader);
			}
		}
		object IEnumerator.Current
		{
			get { return ((IEnumerator<IDataRecord>)this).Current; }
		}
	}
}