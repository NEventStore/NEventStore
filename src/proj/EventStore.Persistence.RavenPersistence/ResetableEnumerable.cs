namespace NEventStore.Persistence.RavenPersistence
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class ResetableEnumerable<T> : IEnumerable<T>
    {
        readonly Func<IEnumerable<T>> source;

        public ResetableEnumerable(Func<IEnumerable<T>> source)
        {
            this.source = source;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ResetableEnumerator<T>(source);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        sealed class ResetableEnumerator<TItem> : IEnumerator<TItem>
        {
            readonly Func<IEnumerable<TItem>> source;
            IEnumerator<TItem> enumerator;

            public ResetableEnumerator(Func<IEnumerable<TItem>> source)
            {
                this.source = source;
            }

            private IEnumerator<TItem> Enumerator
            {
                get { return enumerator ?? (enumerator = source().GetEnumerator()); }
                set { enumerator = value; }
            }

            public TItem Current { get { return Enumerator.Current; } }

            object IEnumerator.Current { get { return Current; } }

            public bool MoveNext()
            {
                return Enumerator.MoveNext();
            }

            public void Reset()
            {
                Enumerator.Dispose();
                Enumerator = null;
            }

            public void Dispose()
            {
                Enumerator.Dispose();
            }
        }
    }
}