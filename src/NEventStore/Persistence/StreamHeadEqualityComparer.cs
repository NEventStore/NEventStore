namespace NEventStore.Persistence
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a strategy for comparing stream heads.
    /// </summary>
    public sealed class StreamHeadEqualityComparer : IEqualityComparer<IStreamHead>
    {
        /// <inheritdoc/>
        public bool Equals(IStreamHead x, IStreamHead y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            if (x is null)
            {
                return false;
            }
            if (y is null)
            {
                return false;
            }
            if (x.GetType() != y.GetType())
            {
                return false;
            }
            return string.Equals(x.StreamId, y.StreamId, StringComparison.Ordinal) && string.Equals(x.BucketId, y.BucketId, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public int GetHashCode(IStreamHead obj)
        {
            unchecked
            {
                return ((obj.StreamId?.GetHashCode() ?? 0) * 397) ^ (obj.BucketId?.GetHashCode() ?? 0);
            }
        }
    }
}