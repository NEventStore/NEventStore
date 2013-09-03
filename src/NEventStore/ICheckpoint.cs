namespace NEventStore
{
    using System;

    /// <summary>
    /// Represents a storage level checkpoint to order commits.
    /// </summary>
    public interface ICheckpoint : IComparable<ICheckpoint>
    {
        string Value { get; }
    }
}