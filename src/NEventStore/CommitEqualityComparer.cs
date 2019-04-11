namespace NEventStore
{
    using System.Collections.Generic;

    public sealed class CommitEqualityComparer : IEqualityComparer<ICommit>
    {
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
            return string.Equals(x.BucketId, y.BucketId) && string.Equals(x.StreamId, y.StreamId) && string.Equals(x.CommitId, y.CommitId) ;
        }

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