namespace EventStore.Persistence.MongoPersistence
{
    using System.Globalization;

    public static class ExtensionMethods
    {
        private const string IdFormat = "{0}.{1}";

        public static string Id(this Commit commit)
        {
            if (commit == null)
                return null;

            return IdFormat.FormatWith(commit.StreamId, commit.CommitSequence);
        }

        public static string FormatWith(this string format, params object[] values)
        {
            return string.Format(CultureInfo.InvariantCulture, format, values);
        }

    }
}