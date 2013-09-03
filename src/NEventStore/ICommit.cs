namespace NEventStore
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    ///     Represents a series of events which have been fully committed as a single unit and which apply to the stream indicated.
    /// </summary>
    public interface ICommit
    {
        /// <summary>
        ///     Gets the value which identifies bucket to which the the stream and the the commit belongs.
        /// </summary>
        string BucketId { get; }

        /// <summary>
        ///     Gets the value which uniquely identifies the stream to which the commit belongs.
        /// </summary>
        string StreamId { get; }

        /// <summary>
        ///     Gets the value which indicates the revision of the most recent event in the stream to which this commit applies.
        /// </summary>
        int StreamRevision { get; }

        /// <summary>
        ///     Gets the value which uniquely identifies the commit within the stream.
        /// </summary>
        Guid CommitId { get; }

        /// <summary>
        ///     Gets the value which indicates the sequence (or position) in the stream to which this commit applies.
        /// </summary>
        int CommitSequence { get; }

        /// <summary>
        ///     Gets the point in time at which the commit was persisted.
        /// </summary>
        DateTime CommitStamp { get; }

        /// <summary>
        ///     Gets the metadata which provides additional, unstructured information about this commit.
        /// </summary>
        IDictionary<string, object> Headers { get; }

        /// <summary>
        ///     Gets the collection of event messages to be committed as a single unit.
        /// </summary>
        ICollection<IEventMessage> Events { get; }

        /// <summary>
        /// The checkpoint that represents the storage level order.
        /// </summary>
        ICheckpoint Checkpoint { get; }
    }

    public interface ICheckpoint : IComparable<ICheckpoint>
    {
        string Value { get; }
    }

    public sealed class IntCheckpoint : ICheckpoint
    {
        private readonly int _value;

        public IntCheckpoint(int value)
        {
            _value = value;
        }

        public string Value { get { return _value.ToString(CultureInfo.InvariantCulture); }}

        public int IntValue { get { return _value; } }

        public int CompareTo(ICheckpoint other)
        {
            if (other == null)
            {
                return 1;
            }
            return _value.CompareTo(other);
        }
    }
}