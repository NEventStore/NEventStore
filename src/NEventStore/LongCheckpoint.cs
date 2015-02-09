namespace NEventStore
{
    using System;
    using System.Globalization;

    public sealed class LongCheckpoint : ICheckpoint, IComparable<LongCheckpoint>
    {
        private readonly long _value;

        public LongCheckpoint(long value)
        {
            _value = value;
        }

        public string Value { get { return _value.ToString(CultureInfo.InvariantCulture); }}

        public long LongValue { get { return _value; } }

        public int CompareTo(ICheckpoint other)
        {
            if (other == null)
            {
                return 1;
            }
            var longCheckpoint = other as LongCheckpoint;
            if (longCheckpoint == null)
            {
                throw new InvalidOperationException("Can only compare with {0} but compared with {1}"
                    .FormatWith(typeof(LongCheckpoint).Name, other.GetType()));
            }
            return _value.CompareTo(longCheckpoint.LongValue);
        }

        public static LongCheckpoint Parse(string checkpointValue)
        {
            return string.IsNullOrWhiteSpace(checkpointValue) ? new LongCheckpoint(-1) : new LongCheckpoint(long.Parse(checkpointValue));
        }

        public int CompareTo(LongCheckpoint other)
        {
            return LongValue.CompareTo(other.LongValue);
        }
    }
}