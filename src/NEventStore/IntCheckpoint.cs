namespace NEventStore
{
    using System;
    using System.Globalization;

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
            var intCheckpoint = other as IntCheckpoint;
            if (intCheckpoint == null)
            {
                throw new InvalidOperationException("Can only compare with {0} but compared with {1}".FormatWith());
            }
            return _value.CompareTo(intCheckpoint.IntValue);
        }

        public static IntCheckpoint Parse(string checkpointValue)
        {
            return string.IsNullOrWhiteSpace(checkpointValue) ? new IntCheckpoint(-1) : new IntCheckpoint(int.Parse(checkpointValue));
        }
    }
}