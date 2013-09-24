namespace NEventStore
{
    using System;
    using System.Globalization;

    public sealed class LongCheckpoint : ICheckpoint
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
            var intCheckpoint = other as LongCheckpoint;
            if (intCheckpoint == null)
            {
                throw new InvalidOperationException("Can only compare with {0} but compared with {1}".FormatWith());
            }
            return _value.CompareTo(intCheckpoint.LongValue);
        }

        public static LongCheckpoint Parse(string checkpointValue)
        {
            return string.IsNullOrWhiteSpace(checkpointValue) ? new LongCheckpoint(-1) : new LongCheckpoint(long.Parse(checkpointValue));
        }
    }
}