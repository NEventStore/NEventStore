namespace NEventStore
{
    /// <summary>
    ///    Compares two commits for equality based on their bucket identity, stream identity, and commit identity.
    /// </summary>
    public sealed class CommitEqualityComparer : IEqualityComparer<ICommit>
    {
        /// <inheritdoc/>
        public bool Equals(ICommit x, ICommit y)
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
            return string.Equals(x.BucketId, y.BucketId, StringComparison.Ordinal)
                && string.Equals(x.StreamId, y.StreamId, StringComparison.Ordinal)
                && string.Equals(x.CommitId, y.CommitId);
        }

        /// <inheritdoc/>
        public int GetHashCode(ICommit obj)
        {
            unchecked
            {
                int hashCode = obj.BucketId?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (obj.StreamId?.GetHashCode() ?? 0);
                return (hashCode * 397) ^ obj.CommitId.GetHashCode();
            }
        }
    }
}