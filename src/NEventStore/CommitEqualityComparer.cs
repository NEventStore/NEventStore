#region

using System.Collections.Generic;

#endregion

namespace NEventStore;

public sealed class CommitEqualityComparer : IEqualityComparer<ICommit>
{
    public bool Equals(ICommit x, ICommit y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return string.Equals(x.BucketId, y.BucketId) && string.Equals(x.StreamId, y.StreamId) &&
               Equals(x.CommitId, y.CommitId);
    }

    public int GetHashCode(ICommit obj)
    {
        unchecked
        {
            var hashCode = obj.BucketId?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ (obj.StreamId?.GetHashCode() ?? 0);
            return (hashCode * 397) ^ obj.CommitId.GetHashCode();
        }
    }
}